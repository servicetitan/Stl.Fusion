namespace Stl.Fusion.Extensions;

public sealed class Connector<TConnection> : WorkerBase
    where TConnection : class
{
    private readonly Func<CancellationToken, Task<TConnection>> _connectionFactory;
    private volatile AsyncEvent<State> _state = new(State.New(), true);

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
        _connectionFactory = connectionFactory;
        IsConnected = stateFactory.NewMutable<bool>();
    }

    public Task<TConnection> GetConnection(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var state = _state;
        var stateValue = state.Value;
        return stateValue.ConnectionTask.IsCompletedSuccessfully()
            ? stateValue.ConnectionTask
            : AwaitConnection();

        async Task<TConnection> AwaitConnection()
        {
            this.Start();
            while (true) {
                try {
                    return await state.Value.ConnectionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException) {
                    state = await state.WhenNext(cancellationToken).ConfigureAwait(false);
                    if (state == null)
                        throw new OperationCanceledException();
                }
            }
        }
    }

    public void DropConnection(TConnection connection, Exception? error)
    {
        AsyncEvent<State> prevState;
        lock (Lock) {
            prevState = _state;
            if (!prevState.Value.ConnectionTask.IsCompleted)
                return; // Nothing to do: not yet connected
#pragma warning disable VSTHRD104
            if (connection != prevState.Value.ConnectionTask.Result)
                return; // The connection is already renewed
#pragma warning restore VSTHRD104

            _state = prevState.AppendNext(State.New() with {
                LastError = error,
                RetryIndex = prevState.Value.RetryIndex + 1,
            });
        }
        prevState.Value.Dispose();
    }

    // Protected & private methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        AsyncEvent<State>? state;
        lock (Lock)
            state = _state;
        while (true) {
            var connectionSource = state.Value.ConnectionSource;
            var connectionTask = connectionSource.Task;
            TConnection? connection = null;
            Exception? error = null;
            try {
                Log?.Log(LogLevel, "{LogTag}: Connecting...", LogTag);
                if (!connectionTask.IsCompleted)
                    connection = await _connectionFactory.Invoke(cancellationToken).ConfigureAwait(false);
                else // Something
                    connection = await connectionTask.ConfigureAwait(false);
                connectionSource.TrySetResult(connection);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                error = e;
                connectionSource.TrySetException(e);
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
                await state.WhenNext(cancellationToken).ConfigureAwait(false);
            }

            lock (Lock) {
                if (state == _state) {
                    _state = state.AppendNext(State.New() with {
                        LastError = error,
                        RetryIndex = state.Value.RetryIndex + 1,
                    });
                    state.Value.Dispose();
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

    protected override Task OnStop()
    {
        lock (Lock) {
            var prevState = _state;
            if (!prevState.Value.ConnectionTask.IsCompleted)
                prevState.Value.ConnectionSource.TrySetCanceled();

            _state = prevState.AppendNext(State.NewCancelled(StopToken));
            _state.Complete(StopToken);
            prevState.Value.Dispose();
        }
        IsConnected.Value = false;
        return Task.CompletedTask;
    }

    // Nested types

    private readonly record struct State(
        TaskCompletionSource<TConnection> ConnectionSource,
        Exception? LastError = null,
        int RetryIndex = 0) : IDisposable
    {
        public Task<TConnection> ConnectionTask => ConnectionSource.Task;

        public static State New()
            => new (TaskCompletionSourceExt.New<TConnection>());
        public static State NewCancelled(CancellationToken cancellationToken)
            => new (TaskCompletionSourceExt.New<TConnection>().WithCancellation(cancellationToken));

        public void Dispose()
        {
            var connectionTask = ConnectionTask;
            if (!ConnectionTask.IsCompletedSuccessfully())
                return;

            // Dispose the connection
            _ = Task.Run(async () => {
                var connection = await connectionTask.ConfigureAwait(false);
                if (connection is IAsyncDisposable ad)
                    _ = ad.DisposeAsync();
                else if (connection is IDisposable d)
                    d.Dispose();
            }, CancellationToken.None);
        }
    }
}
