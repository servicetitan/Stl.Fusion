using Stl.Fusion.UI;

namespace Stl.Fusion;

public interface IUpdateDelayer
{
    ValueTask Delay(int retryCount, CancellationToken cancellationToken = default);
}

public sealed record UpdateDelayer(
    UIActionTracker? UIActionTracker,
    RandomTimeSpan UpdateDelay,
    RetryDelaySeq RetryDelays
    ) : IUpdateDelayer
{
    public IMomentClock Clock { get; init; } = UIActionTracker?.Clock ?? MomentClockSet.Default.CpuClock;
    public TimeSpan MinDelay { get; init; } = Defaults.MinDelay;

    public UpdateDelayer(UIActionTracker uiActionTracker)
        : this(uiActionTracker, Defaults.UpdateDelay, Defaults.RetryDelays) { }
    public UpdateDelayer(UIActionTracker uiActionTracker, RandomTimeSpan updateDelay)
        : this(uiActionTracker, updateDelay, Defaults.RetryDelays) { }

    public async ValueTask Delay(int retryCount, CancellationToken cancellationToken = default)
    {
        var minDelay = retryCount == 0 ? MinDelay : RetryDelays.Min;
        var delay = TimeSpanExt.Max(minDelay, GetDelay(retryCount));
        if (delay <= TimeSpan.Zero)
            return; // This may only happen if MinDelay == 0 - e.g. for UpdateDelayer.ZeroUnsafe

        var clock = Clock;
        var minDelayEndTime = clock.Now + minDelay;

        // Await for either delay or the beginning of a period of instant updates
        if (UIActionTracker != null) {
            var whenInstantUpdatesEnabledTask = UIActionTracker.WhenInstantUpdatesEnabled();
            if (!whenInstantUpdatesEnabledTask.IsCompleted) {
                var cts = cancellationToken.CreateLinkedTokenSource();
                try {
                    await Task.WhenAny(
                        whenInstantUpdatesEnabledTask,
                        clock.Delay(delay, cts.Token)
                        ).ConfigureAwait(false);
                }
                finally {
                    cts.CancelAndDisposeSilently();
                }
            }
        }
        else {
            await clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        // Ensure MinDelay is enforced no matter what, otherwise we might
        // end up in a situation when updates are consuming 100% CPU
        var remainingTime = minDelayEndTime - clock.Now;
        if (remainingTime > TimeSpan.Zero)
            await clock.Delay(remainingTime, cancellationToken).ConfigureAwait(false);
    }

    public TimeSpan GetDelay(int retryCount)
        => retryCount > 0 ? RetryDelays[retryCount] : UpdateDelay.Next();

    // Nested types

    public static class Defaults
    {
        public static RandomTimeSpan UpdateDelay { get; set; } = TimeSpan.FromSeconds(1);

        public static RetryDelaySeq RetryDelays {
            get => FixedDelayer.Defaults.RetryDelays;
            set => FixedDelayer.Defaults.RetryDelays = value;
        }

        public static TimeSpan MinDelay {
            get => FixedDelayer.Defaults.MinDelay;
            set => FixedDelayer.Defaults.MinDelay = value;
        }
    }
}
