using Stl.Fusion.UI;

namespace Stl.Fusion;

public interface IUpdateDelayer
{
    UIActionTracker UIActionTracker { get; }
    IMomentClock Clock { get; }
    TimeSpan MinDelay { get; }
    TimeSpan MinRetryDelay { get; }

    TimeSpan GetDelay(int retryCount);
}

public sealed record UpdateDelayer(
    UIActionTracker UIActionTracker,
    RandomTimeSpan Delay,
    RetryDelaySeq RetryDelays
    ) : IUpdateDelayer
{
    public static class Defaults
    {
        public static RandomTimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
        public static TimeSpan MinDelay { get; set; } = TimeSpan.FromMilliseconds(25);
        public static RetryDelaySeq RetryDelays { get; set; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
    }

    public static UpdateDelayer Instant { get; set; } = new(UIActionTracker.None, Defaults.MinDelay);
    public static UpdateDelayer ZeroUnsafe { get; set; } = new(UIActionTracker.None, 0) { MinDelay = TimeSpan.Zero };

    public IMomentClock Clock { get; init; } = UIActionTracker.Clock;
    public TimeSpan MinDelay { get; init; } = Defaults.MinDelay;
    public TimeSpan MinRetryDelay => RetryDelays.Min;

    public UpdateDelayer(UIActionTracker uiActionTracker)
        : this(uiActionTracker, Defaults.Delay, Defaults.RetryDelays) { }
    public UpdateDelayer(UIActionTracker uiActionTracker, RandomTimeSpan updateDelay)
        : this(uiActionTracker, updateDelay, Defaults.RetryDelays) { }

    public TimeSpan GetDelay(int retryCount) 
        => retryCount > 0 ? RetryDelays[retryCount] : Delay.Next();

    // We want referential equality back for this type:
    // it's a record solely to make it possible to use it with "with" keyword
    public bool Equals(UpdateDelayer? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
