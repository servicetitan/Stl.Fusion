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
        ArgumentSerializer = Hub.ArgumentSerializer;
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
            if (oldError != null && Hub.UnrecoverableErrorDetector.Invoke(oldError, StopToken))
                return;

            var nextState = state.Next(channel, error);
            _connectionState = connectionState.CreateNext(nextState);
            if (oldChannel != null)
                oldChannel.Writer.TryComplete(error);
        }

        if (channel != null)
            Log.LogInformation("'{PeerId}': Connected", Id);
        else {
            if (error != null) {
                if (Hub.UnrecoverableErrorDetector.Invoke(error, StopToken)) {
                    if (StopToken.IsCancellationRequested && error is OperationCanceledException) {
                        Log.LogInformation("'{PeerId}': Can't (re)connect, will shut down: stopped", Id);
                        return;
                    }
                }
                Log.LogInformation(
                    "'{PeerId}': Disconnected: {ErrorType}: {ErrorMessage}",
                    Id, error.GetType().GetName(), error.Message);
            }
            else
                Log.LogInformation("'{PeerId}': Disconnected", Id);
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
                if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken))
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask Send(RpcMessage message)
    {
        // This method is optimized to run as quickly as possible,
        // that's why it is a bit complicated.

        var channelTask = GetChannel(default);
        if (!channelTask.IsCompletedSuccessfully)
            return CompleteTrySend(channelTask, message);

#pragma warning disable VSTHRD103
        var channel = channelTask.Result;
#pragma warning restore VSTHRD103
        try {
            if (channel.Writer.TryWrite(message))
                return default;

            return CompleteTrySend(channel, message);
        }
        catch (Exception) {
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
                foreach (var call in Calls.Outbound.Values) {
                    cancellationToken.ThrowIfCancellationRequested();
                    await call.Send().ConfigureAwait(false);
                }
                var channelReader = channel.Reader;
                if (semaphore == null)
                    while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
                        _ = ProcessMessage(message, null, cancellationToken);
                    }
                else
                    while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
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
                if (Hub.UnrecoverableErrorDetector.Invoke(e, StopToken))
                    throw;
            }
        }
    }

    protected override Task OnStart(CancellationToken cancellationToken)
    {
        Log.LogInformation("'{PeerId}': Started", Id);
        foreach (var peerTracker in Hub.PeerTrackers)
            peerTracker.Invoke(this);
        return Task.CompletedTask;
    }

    protected override Task OnStop()
    {
        Hub.Peers.TryRemove(Id, this);
        _ = DisposeAsync();
        Log.LogInformation("'{PeerId}': Stopped", Id);
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

    // Private methods

    private async ValueTask CompleteTrySend(ValueTask<Channel<RpcMessage>> channelTask, RpcMessage message)
    {
        var channel = await channelTask.ConfigureAwait(false);
        try {
            while (!channel.Writer.TryWrite(message))
                await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
        }
#pragma warning disable RCS1075
        catch (Exception) {
            // Intended
        }
#pragma warning restore RCS1075
    }

    private async ValueTask CompleteTrySend(Channel<RpcMessage> channel, RpcMessage message)
    {
        try {
            // If we're here, WaitToWriteAsync call is required to continue
            await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
            while (!channel.Writer.TryWrite(message))
                await channel.Writer.WaitToWriteAsync(StopToken).ConfigureAwait(false);
        }
#pragma warning disable RCS1075
        catch (Exception) {
            // Intended
        }
#pragma warning restore RCS1075
    }
}
