namespace Stl.Fusion.EntityFramework.Operations;

public abstract record DbOperationCompletionTrackingOptions
{
    public TimeSpan MaxCommitDuration { get; init; } = TimeSpan.FromSeconds(1);
    public int? NotifyRetryCount { get; init; } = 3; 
    public RetryDelaySeq NotifyRetryDelays { get; init; } = (TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1));
    public RetryDelaySeq TrackerRetryDelays { get; init; } = (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
}
