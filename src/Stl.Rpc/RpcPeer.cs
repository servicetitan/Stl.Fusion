using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public abstract class RpcPeer : WorkerBase
{
    private static readonly ConcurrentDictionary<Type, Func<RpcHub, RpcPeerRef, RpcPeer>> FactoryCache = new();

    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<RpcPeerConnectionState> _connectionState = new(RpcPeerConnectionState.Initial, true);

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    public RpcHub Hub { get; }
    public RpcPeerRef Ref { get; }
    public int InboundConcurrencyLevel { get; init; } = 0; // 0 = no concurrency limit, 1 = one call at a time, etc.
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcInboundContextFactory InboundContextFactory { get; init; }
    public RpcInboundCallTracker InboundCalls { get; init; }
    public RpcOutboundCallTracker OutboundCalls { get; init; }
    public AsyncEvent<RpcPeerConnectionState> ConnectionState => _connectionState;

    public static RpcPeer New(RpcHub hub, RpcPeerRef peerRef)
        => FactoryCache.GetOrAdd(peerRef.PeerType,
            static type => (Func<RpcHub, RpcPeerRef, RpcPeer>)
                type.GetConstructorDelegate(typeof(RpcHub), typeof(RpcPeerRef))!
        ).Invoke(hub, peerRef);

    protected RpcPeer(RpcHub hub, RpcPeerRef @ref)
    {
        Hub = hub;
        Ref = @ref;
        ArgumentSerializer = Hub.ArgumentSerializer;
        LocalServiceFilter = null!; // To make sure any descendant has to set it
        InboundContextFactory = Hub.InboundContextFactory;
        InboundCalls = Services.GetRequiredService<RpcInboundCallTracker>();
        InboundCalls.Initialize(this);
        OutboundCalls = Services.GetRequiredService<RpcOutboundCallTracker>();
        OutboundCalls.Initialize(this);
    }

    public void SetConnectionState(Channel<RpcMessage>? channel, Exception? error = null)
    {
        lock (Lock) {
            var connectionState = _connectionState;
            var state = connectionState.Value;
            var (oldChannel, oldError, _) = state;
            if (oldChannel == channel && oldError == error)
                return;
            if (oldError != null && Hub.UnrecoverableErrorDetector.Invoke(oldError, StopToken))
                return;

            var nextState = state.Next(channel, error);
            _connectionState = connectionState.CreateNext(nextState);
            if (oldChannel != null)
                oldChannel.Writer.TryComplete(error);
        }

        if (channel != null)
            Log.LogInformation("'{PeerId}': Connected", Ref);
        else {
            if (error != null) {
                if (Hub.UnrecoverableErrorDetector.Invoke(error, StopToken)) {
                    if (StopToken.IsCancellationRequested && error is OperationCanceledException) {
                        Log.LogInformation("'{PeerId}': Can't (re)connect, will shut down: stopped", Ref);
                        return;
                    }
                }
                Log.LogInformation(
                    "'{PeerId}': Disconnected: {ErrorType}: {ErrorMessage}",
                    Ref, error.GetType().GetName(), error.Message);
            }
            else
                Log.LogInformation("'{PeerId}': Disconnected", Ref);
        }
    }

    public async ValueTask<Channel<RpcMessage>> GetChannel()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionState = ConnectionState;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNextConnectionState = connectionState.WhenNext();
            if (!whenNextConnectionState.IsCompleted) {
                var (channel, error, _) = connectionState.Value;
                if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken))
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.ConfigureAwait(false);
        }
    }

    public ValueTask Send(RpcMessage message)
    {
        // !!! Send should never throw an exception.
        // This method is optimized to run as quickly as possible,
        // that's why it is a bit complicated.

        try {
            var channelTask = GetChannel();
            if (!channelTask.IsCompletedSuccessfully)
                return CompleteTrySend(channelTask, message);

#pragma warning disable VSTHRD103
            var channel = channelTask.Result;
#pragma warning restore VSTHRD103
            if (channel.Writer.TryWrite(message))
                return default;

            return CompleteTrySend(channel, message);
        }
        catch (Exception e) {
            Log.LogError(e, "Send failed");
            return default;
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
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (channel is null or IEmptyChannel)
                    throw Errors.ConnectionUnrecoverable();

                SetConnectionState(channel);
                foreach (var call in OutboundCalls) {
                    cancellationToken.ThrowIfCancellationRequested();
                    await call.Send().ConfigureAwait(false);
                }
                var channelReader = channel.Reader;
                if (semaphore == null)
                    while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
                        _ = ProcessMessage(message, cancellationToken);
                    }
                else
                    while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
                        if (Equals(message.Service, RpcSystemCalls.Name.Value)) {
                            // System calls are exempt from semaphore use
                            _ = ProcessMessage(message, cancellationToken);
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
                if (Hub.UnrecoverableErrorDetector.Invoke(e, StopToken))
                    throw;
            }
        }
    }

    protected override Task OnStart(CancellationToken cancellationToken)
    {
        Log.LogInformation("'{PeerId}': Started", Ref);
        foreach (var peerTracker in Hub.PeerTrackers)
            peerTracker.Invoke(this);
        return Task.CompletedTask;
    }

    protected override Task OnStop()
    {
        Hub.Peers.TryRemove(Ref, this);
        _ = DisposeAsync();
        Log.LogInformation("'{PeerId}': Stopped", Ref);
        return Task.CompletedTask;
    }

    protected async Task ProcessMessage(
        RpcMessage message,
        CancellationToken cancellationToken)
    {
        try {
            var context = InboundContextFactory.Invoke(this, message, cancellationToken);
            using var scope = context.Activate();
            await context.Call.Run().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to process message: {Message}", message);
        }
    }

    protected async Task ProcessMessage(
        RpcMessage message,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try {
            var context = InboundContextFactory.Invoke(this, message, cancellationToken);
            using var scope = context.Activate();
            await context.Call.Run().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to process message: {Message}", message);
        }
        finally {
            semaphore.Release();
        }
    }

    // Private methods

    private async ValueTask CompleteTrySend(ValueTask<Channel<RpcMessage>> channelTask, RpcMessage message)
    {
        // !!! This method should never fail
        try {
            var channel = await channelTask.ConfigureAwait(false);
            while (!channel.Writer.TryWrite(message))
                await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
        }
#pragma warning disable RCS1075
        catch (Exception e) {
            Log.LogError(e, "Send failed");
        }
#pragma warning restore RCS1075
    }

    private async ValueTask CompleteTrySend(Channel<RpcMessage> channel, RpcMessage message)
    {
        // !!! This method should never fail
        try {
            // If we're here, WaitToWriteAsync call is required to continue
            await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
            while (!channel.Writer.TryWrite(message))
                await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
        }
#pragma warning disable RCS1075
        catch (Exception e) {
            Log.LogError(e, "Send failed");
        }
#pragma warning restore RCS1075
    }
}
