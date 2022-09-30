namespace Stl.Fusion.Bridge;

public sealed class Connector<TConnection> : WorkerBase
    where TConnection : class
{
    private readonly Func<CancellationToken, Task<TConnection>> _connectionFactory;
    private volatile ManualAsyncEvent<State> _state;

    public IMutableState<bool> IsConnected { get; }

    public Func<TConnection, CancellationToken, Task>? Connected { get; init; }
    public Func<TConnection?, Exception?, CancellationToken, Task>? Disconnected { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public IMomentClock Clock { get; init; } = MomentClockSet.Default.CpuClock;
    public ILogger? Log { get; init; }
    public LogLevel LogLevel { get; init; } = LogLevel.Debug;
    public string LogTag { get; init; } = "(Unknown)";

    public Connector(
        Func<CancellationToken, Task<TConnection>> connectionFactory,
        IStateFactory stateFactory)
    {
        _state = new ManualAsyncEvent<State>(new(), true);
        _connectionFactory = connectionFactory;
        IsConnected = stateFactory.NewMutable<bool>();
    }

    public Task<TConnection> GetConnection(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var state = (AsyncEvent<State>) _state;
        var stateValue = state.Value;
        return stateValue.ConnectionTask.IsCompletedSuccessfully()
            ? stateValue.ConnectionTask
            : AwaitConnection();

        async Task<TConnection> AwaitConnection()
        {
            Start();
            while (true) {
                try {
                    return await state.Value.ConnectionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException) {
                    state = await state.WhenNext().ConfigureAwait(false);
                }
            }
        }
    }

    public void DropConnection(TConnection connection, Exception? error)
    {
        ManualAsyncEvent<State> prevState;
        lock (Lock) {
            prevState = _state;
            if (!prevState.Value.ConnectionTask.IsCompleted)
                return; // Nothing to do: not yet connected
#pragma warning disable VSTHRD104
            if (connection != prevState.Value.ConnectionTask.Result)
                return; // The connection is already renewed
#pragma warning restore VSTHRD104

            var nextState = prevState.Create(new() {
                LastError = error,
                RetryIndex = prevState.Value.RetryIndex + 1,
            });
            _state = nextState;
            prevState.SetNext(nextState);
        }
        prevState.Value.Dispose();
    }

    // Protected & private methods

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        ManualAsyncEvent<State> state;
        lock (Lock) {
            state = _state;
        }
        while (true) {
            var connectionTask = state.Value.ConnectionTask;
            var connectionTaskSource = TaskSource.For(connectionTask);
            TConnection? connection = null;
            Exception? error = null;
            try {
                Log?.Log(LogLevel, "{LogTag}: Connecting...", LogTag);
                if (!connectionTask.IsCompleted)
                    connection = await _connectionFactory.Invoke(cancellationToken).ConfigureAwait(false);
                else // Something 
                    connection = await connectionTask.ConfigureAwait(false);
                connectionTaskSource.TrySetResult(connection);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                error = e;
                connectionTaskSource.TrySetException(e);
            }

            if (connection != null) {
                IsConnected.Value = true;
                Log?.Log(LogLevel, "{LogTag}: Connected", LogTag);
                try {
                    if (Connected != null)
                        await Connected.Invoke(connection, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException) {
                    Log?.LogWarning(e, "{LogTag}: Connected handler failed", LogTag);
                }
                await state.WhenNext().WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            lock (Lock) {
                if (state == _state) {
                    var nextState = state.Create(new() {
                        LastError = error, 
                        RetryIndex = state.Value.RetryIndex + 1,
                    });
                    _state = nextState;
                    state.SetNext(nextState);
                    state.Value.Dispose();
                    state = nextState;
                }
                else {
                    // It was updated by Reconnect, so we just switch to the new state
                    state = _state;
                    error = state.Value.LastError;
                }
            }

            if (error != null) {
                IsConnected.Error = error;
                Log?.LogError(error, "{LogTag}: Disconnected", LogTag);
            }
            else {
                IsConnected.Value = false;
                Log?.LogError("{LogTag}: Disconnected", LogTag);
            }
            if (Disconnected != null)
                await Disconnected.Invoke(connection, error, cancellationToken).ConfigureAwait(false);

            if (state.Value.RetryIndex > 0) {
                var retryDelay = ReconnectDelays[state.Value.RetryIndex];
                Log?.Log(LogLevel, "{LogTag}: Will reconnect in {RetryDelay}", LogTag, retryDelay.ToShortString());
                await Clock.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected override Task OnStopping()
    {
        lock (Lock) {
            var prevState = _state;
            if (!prevState.Value.ConnectionTask.IsCompleted)
                TaskSource.For(prevState.Value.ConnectionTask).TrySetCanceled();
            var nextState = prevState.Create(new() {
                ConnectionTask = Task.FromCanceled<TConnection>(StopToken),
            });
            nextState.CancelNext(StopToken);
            _state = nextState;
            prevState.SetNext(nextState);
            prevState.Value.Dispose();
        }
        IsConnected.Value = false;
        return Task.CompletedTask;
    }

    // Nested types

    private readonly record struct State(
        Task<TConnection> ConnectionTask,
        Exception? LastError = null,
        int RetryIndex = 0) : IDisposable
    {
        public State() : this(TaskSource.New<TConnection>(true).Task) { }

        public void Dispose()
        {
            var connectionTask = ConnectionTask;
            if (!connectionTask.IsCompletedSuccessfully())
                return;

            // Dispose the connection
            Task.Run(async () => {
                var connection = await connectionTask.ConfigureAwait(false);
                if (connection is IAsyncDisposable ad)
                    _ = ad.DisposeAsync();
                else if (connection is IDisposable d)
                    d.Dispose();
            }, CancellationToken.None);
        }
    }
}
