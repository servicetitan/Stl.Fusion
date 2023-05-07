namespace Stl.Fusion.UI;

public sealed class UIActionFailureTracker : MutableList<IUIActionResult>
{
    public Task WhenTracking { get; }

    public UIActionFailureTracker(UIActionTracker actionTracker)
        => WhenTracking = TrackFailures(actionTracker, actionTracker.StopToken);

    public override string ToString()
        => $"{GetType().GetName()}({Count} item(s))";

    private async Task TrackFailures(UIActionTracker actionTracker, CancellationToken cancellationToken = default)
    {
        var lastResultEvent = actionTracker.LastResultEvent;
        while (true) {
            lastResultEvent = await lastResultEvent.WhenNext(cancellationToken).ConfigureAwait(false);
            var result = lastResultEvent.Value;
            if (result is { HasError: true })
                Add(result);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}
