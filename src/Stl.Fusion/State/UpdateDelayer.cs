using Stl.Fusion.UI;

namespace Stl.Fusion;

public interface IUpdateDelayer
{
    Task Delay(IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default);
}

public record UpdateDelayer : IUpdateDelayer
{
    public static class Defaults
    {
        public static RandomTimeSpan UpdateDelay { get; set; } = TimeSpan.FromSeconds(1);
        public static RetryDelaySeq RetryDelays { get; set; } = new(TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2));
        public static RandomTimeSpan UICommandUpdateDelay { get; set; } =  TimeSpan.FromMilliseconds(50);
        public static TimeSpan UICommandRecencyDelta { get; set; } =  TimeSpan.FromMilliseconds(100);
    }

    public static UpdateDelayer ZeroDelay { get; } = new(UI.UICommandTracker.None, 0, 0);
    public static UpdateDelayer MinDelay { get; } = new(UI.UICommandTracker.None, Defaults.UICommandUpdateDelay);

    public IUICommandTracker UICommandTracker { get; init; }
    public MomentClockSet Clocks => UICommandTracker.Clocks;
    public RandomTimeSpan UpdateDelay { get; init; } = Defaults.UpdateDelay;
    public RetryDelaySeq RetryDelays { get; set; } = Defaults.RetryDelays;
    public RandomTimeSpan UICommandUpdateDelay { get; init; } = Defaults.UICommandUpdateDelay;
    public TimeSpan UICommandRecencyDelta { get; init; } = Defaults.UICommandRecencyDelta;

    public UpdateDelayer(IUICommandTracker uiCommandTracker)
        => UICommandTracker = uiCommandTracker;

    public UpdateDelayer(IUICommandTracker uiCommandTracker, RandomTimeSpan updateDelay)
    {
        UICommandTracker = uiCommandTracker;
        UpdateDelay = updateDelay;
    }

    public UpdateDelayer(
        IUICommandTracker uiCommandTracker,
        RandomTimeSpan updateDelay,
        RandomTimeSpan uiCommandUpdateDelay)
    {
        UICommandTracker = uiCommandTracker;
        UpdateDelay = updateDelay;
        UICommandUpdateDelay = uiCommandUpdateDelay;
    }

    public virtual async Task Delay(
        IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default)
    {
        // 1. The update already happened? No need for delay.
        var whenUpdatedTask = stateSnapshot.WhenUpdated();
        if (whenUpdatedTask.IsCompleted)
            return;

        // 2. Wait a bit to see if the invalidation is caused by a UI command
        var delayStart = Clocks.UIClock.Now;
        var commandCompletedTask = UICommandTracker.LastOrWhenCommandCompleted(UICommandRecencyDelta);
        var updateDelay = UpdateDelay.Next();
        if (updateDelay > TimeSpan.Zero) {
            if (!commandCompletedTask.IsCompleted) {
                var waitDuration = TimeSpanExt.Min(updateDelay, UICommandRecencyDelta);
                await Task.WhenAny(whenUpdatedTask, commandCompletedTask)
                    .WaitResultAsync(Clocks.UIClock, waitDuration, cancellationToken)
                    .ConfigureAwait(false);
                if (whenUpdatedTask.IsCompleted)
                    return;
            }
        }

        // 3. Actual delay
        var retryCount = stateSnapshot.RetryCount;
        var retryDelay = GetDelay(commandCompletedTask.IsCompleted, retryCount);
        var remainingDelay = delayStart + retryDelay - Clocks.UIClock.Now;
        if (remainingDelay < TimeSpan.Zero)
            return;
        await whenUpdatedTask
            .WaitResultAsync(Clocks.UIClock, remainingDelay, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual TimeSpan GetDelay(bool isUICommandCaused, int retryCount)
    {
        var updateDelay = UpdateDelay.Next();
        var uiCommandUpdateDelay = TimeSpanExt.Min(updateDelay, UICommandUpdateDelay.Next());
        var baseDelay = isUICommandCaused ? uiCommandUpdateDelay : updateDelay;
        return retryCount <= 0 ? baseDelay : RetryDelays[retryCount];
    }

    // We want referential equality back for this type:
    // it's a record solely to make it possible to use it with "with" keyword
    public virtual bool Equals(UpdateDelayer? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
