namespace Stl.Net;

public sealed class Connector<TConnection> : WorkerBase
    where TConnection : class
{
    private readonly Func<CancellationToken, Task<TConnection>> _connectionFactory;
    private volatile AsyncEvent<State> _state = new(State.New(), true);
    private long _reconnectsAt;

    public AsyncEvent<Result<bool>> IsConnected { get; private set; } = new(false, true);
    public Moment? ReconnectsAt {
        get {
            var reconnectsAt = Interlocked.Read(ref _reconnectsAt);
            return reconnectsAt == default ? null : new Moment(reconnectsAt);
        }
    }

    public Func<TConnection, CancellationToken, Task>? Connected { get; init; }
    public IRetryDelayer ReconnectDelayer { get; init; } = new RetryDelayer();
    public ILogger? Log { get; init; }
    public LogLevel LogLevel { get; init; } = LogLevel.Debug;
    public string LogTag { get; init; }

    public Connector(Func<CancellationToken, Task<TConnection>> connectionFactory)
    {
        _connectionFactory = connectionFactory;
        LogTag = GetType().GetName();
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
                TryIndex = prevState.Value.TryIndex + 1,
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
                Log.IfEnabled(LogLevel)?.Log(LogLevel, "{LogTag}: Connecting...", LogTag);
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
                lock (Lock)
                    IsConnected = IsConnected.AppendNext(true);

                Log.IfEnabled(LogLevel)?.Log(LogLevel, "{LogTag}: Connected", LogTag);
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
                        TryIndex = state.Value.TryIndex + 1,
                    });
                    state.Value.Dispose();
                }
                else {
                    // It was updated by Reconnect, so we just switch to the new state
                    state = _state;
                    error = state.Value.LastError;
                }

                if (error != null) {
                    IsConnected = IsConnected.AppendNext(Result.Error<bool>(error));
                    Log?.LogError(error, "{LogTag}: Disconnected", LogTag);
                }
                else {
                    IsConnected = IsConnected.AppendNext(false);
                    Log?.LogError("{LogTag}: Disconnected", LogTag);
                }
            }

            if (state.Value.TryIndex is var tryIndex and > 0) {
                var delayLogger = new RetryDelayLogger("reconnect", LogTag, Log, LogLevel);
                var (delayTask, endsAt) = ReconnectDelayer.GetDelay(tryIndex, delayLogger, cancellationToken);
                if (!delayTask.IsCompleted) {
                    Interlocked.Exchange(ref _reconnectsAt, endsAt.EpochOffsetTicks);
                    try {
                        await delayTask.ConfigureAwait(false);
                    }
                    finally {
                        Interlocked.Exchange(ref _reconnectsAt, 0);
                    }
                }
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
            IsConnected = IsConnected.AppendNext(true);
            IsConnected.Complete();
        }
        return Task.CompletedTask;
    }

    // Nested types

    private readonly record struct State(
        TaskCompletionSource<TConnection> ConnectionSource,
        Exception? LastError = null,
        int TryIndex = 0) : IDisposable
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
