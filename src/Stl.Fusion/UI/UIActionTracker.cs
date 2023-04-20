namespace Stl.Fusion.UI;

public class UIActionTracker : ProcessorBase, IHasServices, IDisposable
{
    public record Options {
        public TimeSpan MaxInvalidationDelay { get; init; } = TimeSpan.FromMilliseconds(300);
        public IMomentClock? Clock { get; init; }
    }

    protected long RunningActionCountValue;
    protected volatile ManualAsyncEvent<UIAction?> LastActionEventField;
    protected volatile ManualAsyncEvent<IUIActionResult?> LastResultEventField;

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public IMomentClock Clock { get; }
    public ILogger Log { get; }

    public long RunningActionCount => Interlocked.Read(ref RunningActionCountValue);
    public AsyncEvent<UIAction?> LastActionEvent => LastActionEventField;
    public AsyncEvent<IUIActionResult?> LastResultEvent => LastResultEventField;

    public UIActionTracker(Options options, IServiceProvider services)
    {
        Settings = options;
        Services = services;
        Clock = options.Clock ?? services.Clocks().CpuClock;
        Log = services.LogFor(GetType());

        LastActionEventField = new ManualAsyncEvent<UIAction?>(null, true);
        LastResultEventField = new ManualAsyncEvent<IUIActionResult?>(null, true);
    }

    protected override Task DisposeAsyncCore()
    {
        Interlocked.Exchange(ref RunningActionCountValue, 0);
        return Task.CompletedTask;
    }

    public virtual void Register(UIAction action)
    {
        lock (Lock) {
            if (WhenDisposed != null)
                return;

            Interlocked.Increment(ref RunningActionCountValue);
            try {
                var prevEvent = LastActionEventField;
                var nextEvent = prevEvent.Create(action);
                LastActionEventField = nextEvent;
                prevEvent.SetNext(nextEvent);
            }
            catch (Exception e) {
                // We need to keep this count consistent if above block somehow fails
                Interlocked.Decrement(ref RunningActionCountValue);
                if (e is not OperationCanceledException)
                    Log.LogError("UI action registration failed: {Action}", action);
                throw;
            }
        }

        action.WhenCompleted().ContinueWith(_ => {
            Interlocked.Decrement(ref RunningActionCountValue);
            var result = action.UntypedResult;
            if (result == null) {
                Log.LogError("UI action has completed w/o a result: {Action}", action);
                return;
            }

            lock (Lock) {
                if (WhenDisposed != null)
                    return;

                var prevEvent = LastResultEventField;
                var nextEvent = prevEvent.Create(result);
                LastResultEventField = nextEvent;
                prevEvent.SetNext(nextEvent);
            }
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public bool AreInstantUpdatesEnabled()
    {
        // 1. When any action is running
        if (RunningActionCount > 0)
            return true;

        // 2. When invalidations triggered by the most recent action are still coming
        if (LastResultEvent.Value?.CompletedAt >= Clock.Now - Settings.MaxInvalidationDelay)
            return true;

        return false;
    }

    public Task WhenInstantUpdatesEnabled()
        => AreInstantUpdatesEnabled() ? Task.CompletedTask : LastActionEvent.WhenNext();
}
