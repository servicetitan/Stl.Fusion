using Stl.Channels;
using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<Result<Channel<RpcMessage>?>> _whenConnected = new(null, true);

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    public RpcHub Hub { get; }
    public Symbol Name { get; init; }
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcPeerConnector PeerConnector { get; init; }
    public RpcCallRegistry Calls { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int ReconnectRetryLimit { get; init; } = int.MaxValue;
    public int InboundConcurrencyLevel { get; init; } = 0;

    public RpcPeer(RpcHub hub, Symbol name)
    {
        Hub = hub;
        Name = name;
        ArgumentSerializer = Hub.Configuration.ArgumentSerializer;
        LocalServiceFilter = static _ => true;
        PeerConnector = Hub.PeerConnector;
        Calls = new RpcCallRegistry(this);
    }

    public async ValueTask Send(RpcMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetConnection(cancellationToken).ConfigureAwait(false);
        await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var tryIndex = 0;
        var lastError = (Exception?)null;
        var semaphore = InboundConcurrencyLevel > 1
            ? new SemaphoreSlim(InboundConcurrencyLevel, InboundConcurrencyLevel)
            : null;
        while (true) {
            try {
                var channel = await Reconnect(lastError, tryIndex, cancellationToken).ConfigureAwait(false);
                tryIndex = 0;
                if (semaphore == null)
                    await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                        var context = new RpcInboundContext(this, message);
                        _ = ProcessMessage(context, null, cancellationToken);
                    }
                else
                    await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                        var context = new RpcInboundContext(this, message);
                        if (Equals(message.Service, RpcSystemCalls.Name.Value)) {
                            // System calls are exempt from semaphore use
                            _ = ProcessMessage(context, null, cancellationToken);
                        }
                        else {
                            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            _ = ProcessMessage(context, semaphore, cancellationToken);
                        }
                    }

                throw Errors.ConnectionIsClosed();
            }
            catch (ImpossibleToReconnectException e) {
                SetConnection(null, e);
                return;
            }
            catch (OperationCanceledException) {
                SetConnection(null, Errors.ImpossibleToReconnect());
                return;
            }
            catch (Exception e) {
                lastError = e;
                tryIndex++;
            }
        }
    }

    protected async Task<Channel<RpcMessage>> Reconnect(
        Exception? lastError, int tryIndex, CancellationToken cancellationToken)
    {
        SetConnection(null, lastError);
        if (lastError is ImpossibleToReconnectException) {
            Log.LogWarning("'{Name}': Impossible to reconnect, shutting down", Name);
            throw Errors.ImpossibleToReconnect();
        }
        if (tryIndex >= ReconnectRetryLimit) {
            Log.LogWarning("'{Name}': Reconnect retry limit exceeded", Name);
            throw Errors.ImpossibleToReconnect();
        }

        if (tryIndex == 0)
            Log.LogInformation("'{Name}': Connecting...", Name);
        else  {
            var delay = ReconnectDelays[tryIndex];
            Log.LogInformation("'{Name}': Reconnecting (#{TryIndex}) after {Delay}...", Name, tryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        try {
            var channel = await PeerConnector.Invoke(this, cancellationToken).ConfigureAwait(false);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (channel is null or IEmptyChannel)
                throw Errors.ImpossibleToReconnect();

            SetConnection(channel);
            return channel;
        }
        catch (ImpossibleToReconnectException e) {
            SetConnection(null, e);
            throw;
        }
    }

    protected void SetConnection(Channel<RpcMessage>? channel, Exception? error = null)
    {
        var expectedValue = Result.New(channel, error);
        lock (Lock) {
            var whenConnected = _whenConnected;
            if (whenConnected.Value == expectedValue)
                return;
            if (whenConnected.Value.Error is ImpossibleToReconnectException)
                return;

            _whenConnected = whenConnected.CreateNext(expectedValue);
            if (whenConnected.Value.IsValue(out var oldChannel))
                oldChannel?.Writer.TryComplete();
        }
    }

    protected async ValueTask<Channel<RpcMessage>> GetConnection(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var whenConnected = _whenConnected;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNext = whenConnected.WhenNext();
            if (!whenNext.IsCompleted) {
                var error = whenConnected.Value.Error;
                if (error is ImpossibleToReconnectException)
                    throw error;

                var channel = error == null ? whenConnected.Value.ValueOrDefault : null;
                if (channel != null)
                    return channel;
            }

            whenConnected = await whenNext.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual async Task ProcessMessage(
        RpcInboundContext context,
        SemaphoreSlim? semaphore,
        CancellationToken cancellationToken)
    {
        var scope = context.Activate();
        try {
            await context.ProcessCall(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to process message: {Message}", context.Message);
        }
        finally {
            scope.Dispose();
            semaphore?.Release();
        }
    }
}
