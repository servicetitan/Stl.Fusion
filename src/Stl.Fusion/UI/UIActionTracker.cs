namespace Stl.Fusion.UI;

public sealed class UIActionTracker : ProcessorBase, IHasServices
{
    public sealed record Options {
        public TimeSpan InstantUpdatePeriod { get; init; } = TimeSpan.FromMilliseconds(300);
        public IMomentClock? Clock { get; init; }
    }

    private long _runningActionCount;
    private volatile ManualAsyncEvent<UIAction?> _lastActionEvent;
    private volatile ManualAsyncEvent<IUIActionResult?> _lastResultEvent;

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public IMomentClock Clock { get; }
    public ILogger Log { get; }

    public long RunningActionCount => Interlocked.Read(ref _runningActionCount);
    public AsyncEvent<UIAction?> LastActionEvent => _lastActionEvent;
    public AsyncEvent<IUIActionResult?> LastResultEvent => _lastResultEvent;

    public UIActionTracker(Options options, IServiceProvider services)
    {
        Settings = options;
        Services = services;
        Clock = options.Clock ?? services.Clocks().CpuClock;
        Log = services.LogFor(GetType());

        _lastActionEvent = new ManualAsyncEvent<UIAction?>(null, true);
        _lastResultEvent = new ManualAsyncEvent<IUIActionResult?>(null, true);
    }

    protected override Task DisposeAsyncCore()
    {
        Interlocked.Exchange(ref _runningActionCount, 0);
        _lastActionEvent.CancelNext(StopToken);
        _lastResultEvent.CancelNext(StopToken);
        return Task.CompletedTask;
    }

    public void Register(UIAction action)
    {
        lock (Lock) {
            if (StopToken.IsCancellationRequested)
                return;

            Interlocked.Increment(ref _runningActionCount);

            try {
                var prevEvent = _lastActionEvent;
                var nextEvent = prevEvent.CreateNext(action);
                _lastActionEvent = nextEvent;
                prevEvent.SetNext(nextEvent);
            }
            catch (Exception e) {
                // We need to keep this count consistent if above block somehow fails
                Interlocked.Decrement(ref _runningActionCount);
                if (e is not OperationCanceledException)
                    Log.LogError("UI action registration failed: {Action}", action);
                throw;
            }
        }

        action.WhenCompleted().ContinueWith(_ => {
            lock (Lock) {
                if (StopToken.IsCancellationRequested)
                    return;

                Interlocked.Decrement(ref _runningActionCount);

                var result = action.UntypedResult;
                if (result == null) {
                    Log.LogError("UI action has completed w/o a result: {Action}", action);
                    return;
                }

                var prevEvent = _lastResultEvent;
                var nextEvent = prevEvent.CreateNext(result);
                _lastResultEvent = nextEvent;
                prevEvent.SetNext(nextEvent);
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
