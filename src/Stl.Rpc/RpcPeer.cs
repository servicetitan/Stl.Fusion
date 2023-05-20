using System.Diagnostics.CodeAnalysis;
using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<ConnectionState> _connectionState = new(ConnectionState.Initial, true);

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
        while (true) {
            var channel = await GetConnection(cancellationToken).ConfigureAwait(false);
            try {
                await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                SetConnectionState(null, e);
                if (e is ImpossibleToReconnectException)
                    throw;
            }
        }
    }

    // Protected methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var semaphore = InboundConcurrencyLevel > 1
            ? new SemaphoreSlim(InboundConcurrencyLevel, InboundConcurrencyLevel)
            : null;
        while (true) {
            try {
                var channel = await Reconnect(null, cancellationToken).ConfigureAwait(false);
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
            catch (OperationCanceledException) {
                SetConnectionState(null, Errors.ImpossibleToReconnect());
                return;
            }
            catch (Exception e) {
                SetConnectionState(null, e);
                if (e is ImpossibleToReconnectException)
                    return;
            }
        }
    }

    protected async Task<Channel<RpcMessage>> Reconnect(Exception? error, CancellationToken cancellationToken)
    {
        var connectionState = SetConnectionState(null, error);
        if (error is ImpossibleToReconnectException) {
            Log.LogWarning("'{Name}': Impossible to reconnect, shutting down", Name);
            throw Errors.ImpossibleToReconnect();
        }
        if (connectionState.TryIndex >= ReconnectRetryLimit) {
            Log.LogWarning("'{Name}': Reconnect retry limit exceeded", Name);
            throw Errors.ImpossibleToReconnect();
        }

        if (connectionState.TryIndex == 0)
            Log.LogInformation("'{Name}': Connecting...", Name);
        else  {
            var delay = ReconnectDelays[connectionState.TryIndex];
            Log.LogInformation(
                "'{Name}': Reconnecting (#{TryIndex}) after {Delay}...", 
                Name, connectionState.TryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        try {
            var channel = await PeerConnector.Invoke(this, cancellationToken).ConfigureAwait(false);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (channel is null or IEmptyChannel)
                throw Errors.ImpossibleToReconnect();

            SetConnectionState(channel);
            return channel;
        }
        catch (ImpossibleToReconnectException e) {
            SetConnectionState(null, e);
            throw;
        }
    }

    protected ConnectionState SetConnectionState(Channel<RpcMessage>? channel, Exception? error = null)
    {
        lock (Lock) {
            var connectionState = _connectionState;
            var state = connectionState.Value;
            if (state.Channel == channel && state.Error == error)
                return state;
            if (state.Error is ImpossibleToReconnectException)
                return state;

            var nextState = state.Next(channel, error);
            _connectionState = connectionState.CreateNext(nextState);
            state.Channel?.Writer.TryComplete();
            return nextState;
        }
    }

    protected async ValueTask<Channel<RpcMessage>> GetConnection(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionState = _connectionState;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNextConnectionState = connectionState.WhenNext();
            if (!whenNextConnectionState.IsCompleted) {
                var (channel, error, _) = connectionState.Value;
                if (error is ImpossibleToReconnectException)
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.WaitAsync(cancellationToken).ConfigureAwait(false);
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

    public sealed record ConnectionState(
        Channel<RpcMessage>? Channel = null,
        Exception? Error = null,
        int TryIndex = 0)
    {
        public static readonly ConnectionState Initial = new();

#if NETSTANDARD2_0
        public bool IsConnected(out Channel<RpcMessage>? channel)
#else
        public bool IsConnected([NotNullWhen(true)] out Channel<RpcMessage>? channel)
#endif
        {
            channel = Channel;
            return channel != null;
        }

        public ConnectionState Next(Channel<RpcMessage>? channel, Exception? error)
            => error == null ? Next(channel) : Next(error);

        public ConnectionState Next(Channel<RpcMessage>? channel)
            => new(channel);

        public ConnectionState Next(Exception error)
            => new(null, error, TryIndex + 1);
    }
}
