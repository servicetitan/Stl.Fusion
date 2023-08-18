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
    private volatile AsyncState<UIAction?> _lastAction = new(null, true);
    private volatile AsyncState<IUIActionResult?> _lastResult = new(null, true);

    public Options Settings { get; } = settings;
    public IServiceProvider Services { get; } = services;
    public IMomentClock Clock => _clock ??= Settings.Clock ?? Services.Clocks().CpuClock;
    public ILogger Log => _log ??= Services.LogFor(GetType());

    public long RunningActionCount => Interlocked.Read(ref _runningActionCount);
    public AsyncState<UIAction?> LastAction => _lastAction;
    public AsyncState<IUIActionResult?> LastResult => _lastResult;

    protected override Task DisposeAsyncCore()
    {
        Interlocked.Exchange(ref _runningActionCount, 0);
        var error = new ObjectDisposedException(GetType().Name);
        _lastAction.SetFinal(error);
        _lastResult.SetFinal(error);
        return Task.CompletedTask;
    }

    public void Register(UIAction action)
    {
        lock (Lock) {
            if (StopToken.IsCancellationRequested)
                return;

            Interlocked.Increment(ref _runningActionCount);
            try {
                _lastAction = _lastAction.SetNext(action);
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
                _lastResult = _lastResult.TrySetNext(result);
            }
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public bool AreInstantUpdatesEnabled()
    {
        if (RunningActionCount > 0)
            return true;

        if (LastResult.Value is not { } lastResult)
            return false;

        return lastResult.CompletedAt + Settings.InstantUpdatePeriod >= Clock.Now;
    }

    public Task WhenInstantUpdatesEnabled()
        => AreInstantUpdatesEnabled() ? Task.CompletedTask : LastAction.WhenNext();
}
