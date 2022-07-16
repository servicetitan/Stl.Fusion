namespace Stl.Fusion.UI;

public abstract class UIAction : IHasId<long>
{
    private static long _nextId;

    public long Id { get; } = Interlocked.Increment(ref _nextId);
    public ICommand Command { get; }
    public Moment StartedAt { get; }
    public CancellationToken CancellationToken { get; }

    public abstract IUIActionResult? UntypedResult { get; }
    public abstract Task WhenCompleted();

    protected UIAction(ICommand command, Moment startedAt, CancellationToken cancellationToken)
    {
        StartedAt = startedAt;
        Command = command;
        CancellationToken = cancellationToken;
    }

    public override string ToString()
        => $"{GetType().Name}(#{Id}: {Command}, {UntypedResult?.ToString() ?? "still running"})";
}

public class UIAction<TResult> : UIAction
{
    public Task<UIActionResult<TResult>> ResultTask { get; }

    // Computed properties
    public override IUIActionResult? UntypedResult => Result;
    public UIActionResult<TResult>? Result => ResultTask.IsCompleted ? ResultTask.Result : null;

    protected UIAction(ICommand<TResult> command, IMomentClock clock, CancellationToken cancellationToken)
        : base(command, clock.Now, cancellationToken)
        => ResultTask = null!;

    public UIAction(ICommand<TResult> command, IMomentClock clock, Task<TResult> resultTask, CancellationToken cancellationToken)
        : base(command, clock.Now, cancellationToken)
    {
        ResultTask = resultTask.ContinueWith(
            t => {
                var result = t.ToResultSynchronously();
                var completedAt = clock.Now;
                return new UIActionResult<TResult>(this, result, completedAt);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public override Task WhenCompleted()
        => ResultTask;
}
