using System.Diagnostics.CodeAnalysis;
using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public abstract class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<ConnectionState> _connectionState = new(ConnectionState.Initial, true);

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    public RpcHub Hub { get; }
    public Symbol Name { get; }
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcInboundContextFactory InboundContextFactory { get; init; }
    public RpcCallRegistry Calls { get; init; }
    public int InboundConcurrencyLevel { get; init; } = 0;

    protected RpcPeer(RpcHub hub, Symbol name)
    {
        Hub = hub;
        Name = name;
        ArgumentSerializer = Hub.Configuration.ArgumentSerializer;
        LocalServiceFilter = null!; // To make sure any descendant has to set it
        InboundContextFactory = Hub.InboundContextFactory;
        Calls = new RpcCallRegistry(this);
    }

    public async ValueTask Send(RpcMessage message, CancellationToken cancellationToken)
    {
        while (true) {
            var channel = await GetChannel(cancellationToken).ConfigureAwait(false);
            try {
                await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                SetConnectionState(null, e);
                if (e is ImpossibleToConnectException)
                    throw;
            }
        }
    }

    public void SetChannel(Channel<RpcMessage> channel)
    {
        lock (Lock) {
            var connectionState = GetConnectionState();
            if (connectionState.IsConnected(out var existingChannel))
                existingChannel!.Writer.TryComplete(new OperationCanceledException());

            SetConnectionState(channel);
        }
    }

    public ValueTask<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken)
        => GetChannel(Timeout.InfiniteTimeSpan, cancellationToken);
    public async ValueTask<Channel<RpcMessage>> GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionState = _connectionState;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNextConnectionState = connectionState.WhenNext();
            if (!whenNextConnectionState.IsCompleted) {
                var (channel, error, _) = connectionState.Value;
                if (error is ImpossibleToConnectException or TimeoutException)
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
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
                foreach (var call in Calls.Outbound.Values)
                    await call.Send(true).ConfigureAwait(false);

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

                throw Errors.ConnectionIsClosed();
            }
            catch (Exception e) {
                SetConnectionState(null, e);
                if (e is OperationCanceledException or ImpossibleToConnectException)
                    throw;
            }
        }
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

    protected ConnectionState GetConnectionState()
    {
        lock (Lock)
            return _connectionState.Value;
    }

    protected void SetConnectionState(Channel<RpcMessage>? channel, Exception? error = null)
    {
        lock (Lock) {
            var connectionState = _connectionState;
            var state = connectionState.Value;
            if (state.Channel == channel && state.Error == error)
                return;
            if (state.Error is ImpossibleToConnectException)
                return;

            var nextState = state.Next(channel, error);
            _connectionState = connectionState.CreateNext(nextState);
            state.Channel?.Writer.TryComplete();
        }
    }

    // Nested types

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
