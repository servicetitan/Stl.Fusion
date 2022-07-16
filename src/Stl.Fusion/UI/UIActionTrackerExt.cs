namespace Stl.Fusion.UI;

public static class UIActionTrackerExt
{
    public static async Task<IUIActionResult> WhenNextOrRecentResult(
        this UIActionTracker uiActionTracker,
        TimeSpan maxRecency)
    {
        var cutoff = uiActionTracker.Clocks.UIClock.Now - maxRecency;
        var lastResultEvent = uiActionTracker.LastResultEvent;
        var lastResult = lastResultEvent.Value;
        if (lastResult?.CompletedAt >= cutoff)
            return lastResult;
        lastResultEvent = await lastResultEvent.WhenNext().ConfigureAwait(false);
        return lastResultEvent.Value!;
    }
}
