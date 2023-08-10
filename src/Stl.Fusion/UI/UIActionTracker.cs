namespace Stl.Fusion.UI;

public sealed class UIActionTracker(
    UIActionTracker.Options settings,
    IServiceProvider services
    ) : ProcessorBase, IHasServices
{
    public sealed record Options {
        public TimeSpan InstantUpdatePeriod { get; init; } = TimeSpan.FromMilliseconds(300);
        public IMomentClock? Clock { get; init; }
    }

    private IMomentClock? _clock;
    private ILogger? _log;
    private long _runningActionCount;
    private volatile AsyncEvent<UIAction?> _lastActionEvent = new(null, true);
    private volatile AsyncEvent<IUIActionResult?> _lastResultEvent = new(null, true);

    public Options Settings { get; } = settings;
    public IServiceProvider Services { get; } = services;
    public IMomentClock Clock => _clock ??= Settings.Clock ?? Services.Clocks().CpuClock;
    public ILogger Log => _log ??= Services.LogFor(GetType());

    public long RunningActionCount => Interlocked.Read(ref _runningActionCount);
    public AsyncEvent<UIAction?> LastActionEvent => _lastActionEvent;
    public AsyncEvent<IUIActionResult?> LastResultEvent => _lastResultEvent;

    protected override Task DisposeAsyncCore()
    {
        Interlocked.Exchange(ref _runningActionCount, 0);
        var error = new ObjectDisposedException(GetType().Name);
        _lastActionEvent.Complete(error);
        _lastResultEvent.Complete(error);
        return Task.CompletedTask;
    }

    public void Register(UIAction action)
    {
        lock (Lock) {
            if (StopToken.IsCancellationRequested)
                return;

            Interlocked.Increment(ref _runningActionCount);
            try {
                _lastActionEvent = _lastActionEvent.AppendNext(action);
            }
            catch (Exception e) {
                // We need to keep this count consistent if above block somehow fails
                Interlocked.Decrement(ref _runningActionCount);
                if (e is InvalidOperationException)
                    return; // Already stopped

                Log.LogError("UI action registration failed: {Action}", action);
                throw;
            }
        }

        _ = action.WhenCompleted().ContinueWith(_ => {
            lock (Lock) {
                if (StopToken.IsCancellationRequested)
                    return;

                Interlocked.Decrement(ref _runningActionCount);

                var result = action.UntypedResult;
                if (result == null) {
                    Log.LogError("UI action has completed w/o a result: {Action}", action);
                    return;
                }
                _lastResultEvent = _lastResultEvent.TryAppendNext(result);
            }
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public bool AreInstantUpdatesEnabled()
    {
        if (RunningActionCount > 0)
            return true;

        if (LastResultEvent.Value is not { } lastResult)
            return false;

        return lastResult.CompletedAt + Settings.InstantUpdatePeriod >= Clock.Now;
    }

    public Task WhenInstantUpdatesEnabled()
        => AreInstantUpdatesEnabled() ? Task.CompletedTask : LastActionEvent.WhenNext();
}
