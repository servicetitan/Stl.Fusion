namespace Stl.Fusion;

public static class UpdateDelayerExt
{
    public static async ValueTask Delay(
        this IUpdateDelayer updateDelayer,
        int retryCount,
        CancellationToken cancellationToken = default)
    {
        var minDelay = retryCount == 0 ? updateDelayer.MinDelay : updateDelayer.MinRetryDelay;
        var delay = TimeSpanExt.Max(minDelay, updateDelayer.GetDelay(retryCount));
        if (delay <= TimeSpan.Zero)
            return; // This may only happen if MinDelay == 0 - e.g. for UpdateDelayer.ZeroUnsafe

        var clock = updateDelayer.Clock;
        var minDelayEndTime = clock.Now + minDelay;

        // Await for either delay or the beginning of a period of instant updates
        var cts = cancellationToken.CreateLinkedTokenSource();
        try {
            var task1 = clock.Delay(delay, cts.Token);
            var task2 = updateDelayer.UIActionTracker.WhenInstantUpdatesEnabled();
            await Task.WhenAny(task1, task2).ConfigureAwait(false);
        }
        finally {
            cts.CancelAndDisposeSilently();
        }

        // Ensure MinDelay is enforced no matter what, otherwise we might
        // end up in a situation when updates are consuming 100% CPU
        var remainingTime = minDelayEndTime - clock.Now;
        if (remainingTime > TimeSpan.Zero)
            await clock.Delay(remainingTime, cancellationToken).ConfigureAwait(false);
    }
}
