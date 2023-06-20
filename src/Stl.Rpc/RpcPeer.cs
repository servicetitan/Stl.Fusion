using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public abstract class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private AsyncEvent<RpcPeerConnectionState> _connectionState = new(RpcPeerConnectionState.Initial, true);
    private ChannelWriter<RpcMessage>? _sender;

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;
    protected ChannelWriter<RpcMessage>? Sender => _sender;

    public RpcHub Hub { get; }
    public RpcPeerRef Ref { get; }
    public int InboundConcurrencyLevel { get; init; } = 0; // 0 = no concurrency limit, 1 = one call at a time, etc.
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcInboundContextFactory InboundContextFactory { get; init; }
    public RpcInboundCallTracker InboundCalls { get; init; }
    public RpcOutboundCallTracker OutboundCalls { get; init; }
    public AsyncEvent<RpcPeerConnectionState> ConnectionState => _connectionState;

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

    public ValueTask Send(RpcMessage message)
    {
        // !!! Send should never throw an exception.
        // This method is optimized to run as quickly as possible,
        // that's why it is a bit complicated.

        var sendChannel = Sender;
        try {
            if (sendChannel == null || sendChannel.TryWrite(message))
                return default;

            return CompleteTrySend(sendChannel, message);
        }
        catch (Exception e) {
            Log.LogError(e, "Send failed");
            return default;
        }
    }

    public void Disconnect(Exception? error = null)
    {
        ChannelWriter<RpcMessage>? sender;
        lock (Lock) {
            sender = _sender;
            _sender = null;
        }
        sender?.TryComplete(error);
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        AsyncEvent<RpcPeerConnectionState> connectionState;
        lock (Lock) {
            // We want to make sure ConnectionState doesn't change while this method runs
            // and no one else cancels ReaderAbortSource
            connectionState = _connectionState;
            var readerAbortSource = connectionState.Value.ReaderAbortSource;
            if (readerAbortSource == null || readerAbortSource.IsCancellationRequested)
                return;

            readerAbortSource.CancelAndDisposeSilently();
        }

        // There going to be at least one ConnectionState change when the abort is processed,
        // see the last "catch" block in Run method to understand why.
        await connectionState.WhenNext(cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected abstract Task<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken);

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var semaphore = InboundConcurrencyLevel > 1
            ? new SemaphoreSlim(InboundConcurrencyLevel, InboundConcurrencyLevel)
            : null;
        while (true) {
            var readerAbortToken = CancellationToken.None;
            Channel<RpcMessage>? channel = null;
            try {
                channel = await GetChannel(cancellationToken).ConfigureAwait(false);
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (channel is null)
                    throw Errors.ConnectionUnrecoverable();

                var connectionState = SetConnectionState(channel, null, true);
                readerAbortToken = connectionState.ReaderAbortSource!.Token;

                // Recovery: let's re-send all outbound calls
                foreach (var call in OutboundCalls) {
                    readerAbortToken.ThrowIfCancellationRequested();
                    await call.SendRegistered().ConfigureAwait(false);
                }

                var channelReader = channel.Reader;
                if (semaphore == null)
                    while (await channelReader.WaitToReadAsync(readerAbortToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
                        _ = ProcessMessage(message, cancellationToken);
                    }
                else
                    while (await channelReader.WaitToReadAsync(readerAbortToken).ConfigureAwait(false))
                    while (channelReader.TryRead(out var message)) {
                        if (Equals(message.Service, RpcSystemCalls.Name.Value)) {
                            // System calls are exempt from semaphore use
                            _ = ProcessMessage(message, cancellationToken);
                        }
                        else {
                            await semaphore.WaitAsync(readerAbortToken).ConfigureAwait(false);
                            _ = ProcessMessage(message, semaphore, cancellationToken);
                        }
                    }
                SetConnectionState(null, null);
            }
            catch (Exception e) {
                var isReaderAbort = e is OperationCanceledException
                    && readerAbortToken.IsCancellationRequested
                    && !cancellationToken.IsCancellationRequested;
                if (isReaderAbort) {
                    // We can continue using the same channel, but we need to call
                    // SetConnectionState to nullify ReaderAbortSource there & trigger
                    // at least one state change - see Reset method code to understand why.
                    SetConnectionState(channel, null);
                    Log.LogInformation("'{PeerId}': Reset", Ref);
                }
                else {
                    // Resetting sender isn't necessary here - SetConnectionState
                    // will do it automatically
                    SetConnectionState(null, e);
                }
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

    protected RpcPeerConnectionState SetConnectionState(Channel<RpcMessage>? channel, Exception? error, bool renewReaderAbortSource = false)
    {
        if (error != null)
            channel = null;

        Monitor.Enter(Lock);
        var connectionState = _connectionState.LatestOrThrow();
        var oldState = connectionState.Value;
        var state = oldState;
        Exception? terminalError = null;
        try {
            var readerAbortSource = state.ReaderAbortSource;
            if (channel == null || readerAbortSource?.IsCancellationRequested == true)
                readerAbortSource = null;
            else if (renewReaderAbortSource)
                readerAbortSource = StopToken.CreateLinkedTokenSource();
            var tryIndex = error == null ? 0 : state.TryIndex + 1;

            // Let's check if any changes are made at all
            if (oldState.Channel == channel
                && oldState.Error == error
                && oldState.ReaderAbortSource == readerAbortSource
                && oldState.TryIndex == tryIndex)
                return connectionState.Value; // Nothing is changed

            state = new RpcPeerConnectionState(channel, error, readerAbortSource, tryIndex);
            _connectionState = connectionState = connectionState.AppendNext(state);

            if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken)) {
                terminalError = error is ConnectionUnrecoverableException
                    ? error
                    : Errors.ConnectionUnrecoverable(error);
                connectionState.Complete(terminalError);
                throw terminalError;
            }

            return connectionState.Value;
        }
        finally {
            if (state.ReaderAbortSource != oldState.ReaderAbortSource) {
                oldState.ReaderAbortSource.CancelAndDisposeSilently();
                _sender = state.Channel?.Writer;
            }
            if (state.Channel != oldState.Channel)
                oldState.Channel?.Writer.TryComplete(error); // Reliably shut down the old channel
            Monitor.Exit(Lock);

            // The code below is responsible solely for logging - all important stuff is already done
            if (terminalError != null)
                Log.LogInformation("'{PeerId}': Can't (re)connect, will shut down", Ref);
            else if (state.IsConnected() != oldState.IsConnected()) {
                if (state.IsConnected())
                    Log.LogInformation("'{PeerId}': Connected", Ref);
                else {
                    var e = state.Error;
                    if (e != null)
                        Log.LogInformation(
                            "'{PeerId}': Disconnected: {ErrorType}: {ErrorMessage}",
                            Ref, e.GetType().GetName(), e.Message);
                    else
                        Log.LogInformation("'{PeerId}': Disconnected", Ref);
                }
            }
        }
    }

    private async ValueTask CompleteTrySend(ChannelWriter<RpcMessage> sendChannel, RpcMessage message)
    {
        // !!! This method should never fail
        try {
            // If we're here, WaitToWriteAsync call is required to continue
            await sendChannel.WaitToWriteAsync(StopToken).ConfigureAwait(false);
            while (!sendChannel.TryWrite(message))
                await sendChannel.WaitToWriteAsync(StopToken).ConfigureAwait(false);
        }
#pragma warning disable RCS1075
        catch (Exception e) {
            Log.LogError(e, "Send failed");
        }
#pragma warning restore RCS1075
    }
}
