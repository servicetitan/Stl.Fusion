using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public abstract class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private readonly Lazy<ILogger?> _callLogLazy;
    private AsyncState<RpcPeerConnectionState> _connectionState = new(RpcPeerConnectionState.Initial, true);
    private ChannelWriter<RpcMessage>? _sender;

    protected IServiceProvider Services => Hub.Services;
    protected internal ILogger Log => _log ??= Services.LogFor(GetType());
    protected internal ILogger? CallLog => _callLogLazy.Value;
    protected internal ChannelWriter<RpcMessage>? Sender => _sender;

    public RpcHub Hub { get; }
    public RpcPeerRef Ref { get; }
    public int InboundConcurrencyLevel { get; init; } = 0; // 0 = no concurrency limit, 1 = one call at a time, etc.
    public RpcArgumentSerializer ArgumentSerializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RpcInboundContextFactory InboundContextFactory { get; init; }
    public RpcInboundCallTracker InboundCalls { get; init; }
    public RpcOutboundCallTracker OutboundCalls { get; init; }
    public RpcIncomingStreamTracker IncomingStreams { get; init; }
    public RpcOutgoingStreamTracker OutgoingStreams { get; init; }
    public LogLevel CallLogLevel { get; init; } = LogLevel.None;
    public AsyncState<RpcPeerConnectionState> ConnectionState => _connectionState;
    public RpcPeerInternalServices InternalServices => new(this);

    protected RpcPeer(RpcHub hub, RpcPeerRef @ref)
    {
        var services = hub.Services;
        Hub = hub;
        Ref = @ref;
        _callLogLazy = new Lazy<ILogger?>(() => Log.IfEnabled(CallLogLevel), LazyThreadSafetyMode.PublicationOnly);

        ArgumentSerializer = Hub.ArgumentSerializer;
        LocalServiceFilter = null!; // To make sure any descendant has to set it
        InboundContextFactory = Hub.InboundContextFactory;
        InboundCalls = services.GetRequiredService<RpcInboundCallTracker>();
        InboundCalls.Initialize(this);
        OutboundCalls = services.GetRequiredService<RpcOutboundCallTracker>();
        OutboundCalls.Initialize(this);
        IncomingStreams = services.GetRequiredService<RpcIncomingStreamTracker>();
        IncomingStreams.Initialize(this);
        OutgoingStreams = services.GetRequiredService<RpcOutgoingStreamTracker>();
        OutgoingStreams.Initialize(this);
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

            return CompleteSend(sendChannel, message);
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
        AsyncState<RpcPeerConnectionState> connectionState;
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
        // see the last "catch" block in the Run method to understand why.
        await connectionState.WhenNext(cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected abstract Task<RpcConnection> GetConnection(CancellationToken cancellationToken);

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var semaphore = InboundConcurrencyLevel > 1
            ? new SemaphoreSlim(InboundConcurrencyLevel, InboundConcurrencyLevel)
            : null;
        while (true) {
            var readerAbortToken = CancellationToken.None;
            RpcConnection? connection = null;
            try {
                connection = await GetConnection(cancellationToken).ConfigureAwait(false);
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (connection is null)
                    throw Errors.ConnectionUnrecoverable();

                var connectionState = SetConnectionState(connection, null, true);
                readerAbortToken = connectionState.ReaderAbortSource!.Token;

                // Recovery: let's re-send all outbound calls
                foreach (var call in OutboundCalls) {
                    readerAbortToken.ThrowIfCancellationRequested();
                    await call.SendRegistered(true).ConfigureAwait(false);
                }

                var channelReader = connection.Channel.Reader;
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
                    SetConnectionState(connection, null);
                    Log.LogInformation("'{PeerRef}': Reset", Ref);
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
        Log.LogInformation("'{PeerRef}': Started", Ref);
        foreach (var peerTracker in Hub.PeerTrackers)
            peerTracker.Invoke(this);
        return Task.CompletedTask;
    }

    protected override async Task OnStop()
    {
        _ = DisposeAsync();
        Hub.Peers.TryRemove(Ref, this);

        // 1. We want to make sure the sequence of ConnectionStates terminates for sure
        Exception error;
        Monitor.Enter(Lock);
        try {
            if (_connectionState.IsFinal)
                error = _connectionState.Value.Error
                    ?? Stl.Internal.Errors.InternalError(
                        "ConnectionState.IsFinal == true, but ConnectionState.Value.Error == null.");
            else {
                error = Errors.ConnectionUnrecoverable(_connectionState.Value.Error);
                SetConnectionState(null, error);
            }
        }
        catch (Exception e) {
            // Not sure how we might land here, but we still need to report an error, so...
            error = e;
        }
        finally {
            Monitor.Exit(Lock);
        }

        // 2. And we must abort all outbound calls and streams.
        // Inbound calls are auto-aborted via StopToken,
        // which becomes RpcInboundCallContext.CancellationToken.
        var abortCallsTask = OutboundCalls.Abort(error);
        var abortStreamsTask = OutgoingStreams.Abort(error);
        var outboundCallCount = await abortCallsTask.ConfigureAwait(false);
        var outgoingStreamCount = await abortStreamsTask.ConfigureAwait(false);
        Log.LogInformation(
            "'{PeerRef}': Stopped, aborted {OutboundCallCount} outbound call(s), {OutgoingStreamCount} stream(s)",
            Ref, outboundCallCount, outgoingStreamCount);
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

    protected RpcPeerConnectionState SetConnectionState(RpcConnection? connection, Exception? error, bool renewReaderAbortSource = false)
    {
        if (error != null)
            connection = null;

        Monitor.Enter(Lock);
        var connectionState = _connectionState.Last;
        var oldState = connectionState.Value;
        var state = oldState;
        Exception? terminalError = null;
        try {
            var readerAbortSource = state.ReaderAbortSource;
            if (connection == null || readerAbortSource?.IsCancellationRequested == true)
                readerAbortSource = null;
            else if (renewReaderAbortSource)
                readerAbortSource = StopToken.CreateLinkedTokenSource();
            var tryIndex = error == null ? 0 : state.TryIndex + 1;

            // Let's check if any changes are made at all
            if (oldState.Connection == connection
                && oldState.Error == error
                && oldState.ReaderAbortSource == readerAbortSource
                && oldState.TryIndex == tryIndex)
                return connectionState.Value; // Nothing is changed

            state = new RpcPeerConnectionState(connection, error, readerAbortSource, tryIndex);
            _connectionState = connectionState = connectionState.SetNext(state);

            if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken)) {
                terminalError = error is ConnectionUnrecoverableException
                    ? error
                    : Errors.ConnectionUnrecoverable(error);
                connectionState.TrySetFinal(terminalError);
                throw terminalError;
            }

            return connectionState.Value;
        }
        finally {
            if (state.ReaderAbortSource != oldState.ReaderAbortSource) {
                oldState.ReaderAbortSource.CancelAndDisposeSilently();
                _sender = state.Channel?.Writer;
            }
            if (state.Connection != oldState.Connection)
                oldState.Channel?.Writer.TryComplete(error); // Reliably shut down the old channel
            Monitor.Exit(Lock);

            // The code below is responsible solely for logging - all important stuff is already done
            if (terminalError != null)
                Log.LogInformation("'{PeerRef}': Can't (re)connect, will shut down", Ref);
            else if (state.IsConnected())
                Log.LogInformation("'{PeerRef}': Connected", Ref);
            else {
                var e = state.Error;
                if (e != null)
                    Log.LogWarning(
                        "'{PeerRef}': Disconnected: {ErrorType}: {ErrorMessage}",
                        Ref, e.GetType().GetName(), e.Message);
                else
                    Log.LogWarning("'{PeerRef}': Disconnected", Ref);
            }
        }
    }

    private async ValueTask CompleteSend(ChannelWriter<RpcMessage> sendChannel, RpcMessage message)
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
