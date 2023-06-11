using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public abstract class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<RpcPeerConnectionState> _connectionState = new(RpcPeerConnectionState.Initial, true);

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    public RpcHub Hub { get; }
    public Symbol Id { get; }
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcInboundContextFactory InboundContextFactory { get; init; }
    public RpcCallRegistry Calls { get; init; }
    public int InboundConcurrencyLevel { get; init; } = 0;
    public AsyncEvent<RpcPeerConnectionState> ConnectionState => _connectionState;

    protected RpcPeer(RpcHub hub, Symbol id)
    {
        Hub = hub;
        Id = id;
        ArgumentSerializer = Hub.Configuration.ArgumentSerializer;
        LocalServiceFilter = null!; // To make sure any descendant has to set it
        InboundContextFactory = Hub.InboundContextFactory;
        Calls = new RpcCallRegistry(this);
    }

    public void SetConnectionState(Channel<RpcMessage>? channel, Exception? error = null)
    {
        lock (Lock) {
            var connectionState = _connectionState;
            var state = connectionState.Value;
            var (oldChannel, oldError, _) = state;
            if (oldChannel == channel && oldError == error)
                return;
            if (oldError != null && Hub.ErrorClassifier.IsUnrecoverableError(oldError))
                return;

            var nextState = state.Next(channel, error);
            _connectionState = connectionState.CreateNext(nextState);
            state.Channel?.Writer.TryComplete(error);
        }

        if (channel != null)
            Log.LogInformation("'{Id}': Connected", Id);
        else {
            if (error != null) {
                var isTerminalError = Hub.ErrorClassifier.IsUnrecoverableError(error);
                Log.LogWarning(error, isTerminalError
                        ? "'{Id}': Can't (re)connect, will shut down"
                        : "'{Id}': Disconnected",
                    Id);
            }
            else
                Log.LogInformation("'{Id}': Disconnected", Id);
        }
    }

    public ValueTask<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken)
        => GetChannel(Timeout.InfiniteTimeSpan, cancellationToken);
    public async ValueTask<Channel<RpcMessage>> GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionState = ConnectionState;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNextConnectionState = connectionState.WhenNext();
            if (!whenNextConnectionState.IsCompleted) {
                var (channel, error, _) = connectionState.Value;
                if (error != null && Hub.ErrorClassifier.IsUnrecoverableError(error))
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<bool> TrySend(RpcMessage message)
    {
        var channel = await GetChannel(default).ConfigureAwait(false);
        try {
            await channel.Writer.WriteAsync(message).ConfigureAwait(false);
            return ConnectionState.Value.Channel == channel;
        }
        catch (Exception) {
            return false;
        }
    }

    public async ValueTask Send(RpcMessage message)
    {
        while (!await TrySend(message).ConfigureAwait(false)) { }
    }

    // Protected methods

    protected abstract Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken);

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var semaphore = InboundConcurrencyLevel > 1
            ? new SemaphoreSlim(InboundConcurrencyLevel, InboundConcurrencyLevel)
            : null;
        while (true) {
            try {
                var channel = await GetChannelOrReconnect(cancellationToken).ConfigureAwait(false);
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (channel is null or IEmptyChannel)
                    throw Errors.ConnectionUnrecoverable();

                SetConnectionState(channel);
                foreach (var call in Calls.Outbound.Values) {
                    cancellationToken.ThrowIfCancellationRequested();
                    await call.Send().ConfigureAwait(false);
                }

                if (semaphore == null)
                    await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                        _ = ProcessMessage(message, null, cancellationToken);
                    }
                else
                    await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                        if (Equals(message.Service, RpcSystemCalls.Name.Value)) {
                            // System calls are exempt from semaphore use
                            _ = ProcessMessage(message, null, cancellationToken);
                        }
                        else {
                            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            _ = ProcessMessage(message, semaphore, cancellationToken);
                        }
                    }
                SetConnectionState(null);
            }
            catch (Exception e) {
                SetConnectionState(null, e);
                if (Hub.ErrorClassifier.IsUnrecoverableError(e))
                    throw;
            }
        }
    }

    protected override Task OnStart(CancellationToken cancellationToken)
    {
        Log.LogInformation("'{Id}': Started", Id);
        foreach (var peerTracker in Hub.PeerTrackers)
            peerTracker.Invoke(this);
        return Task.CompletedTask;
    }

    protected override Task OnStop()
    {
        Hub.Peers.TryRemove(Id, this);
        _ = DisposeAsync();
        Log.LogInformation("'{Id}': Stopped", Id);
        return Task.CompletedTask;
    }

    protected async Task ProcessMessage(
        RpcMessage message,
        SemaphoreSlim? semaphore,
        CancellationToken cancellationToken)
    {
        var context = InboundContextFactory.Invoke(this, message, cancellationToken);
        var scope = context.Activate();
        try {
            await context.Call.Invoke().ConfigureAwait(false);
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
