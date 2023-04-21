namespace Stl.Fusion;

public sealed record FixedDelayer(
    TimeSpan UpdateDelay,
    RetryDelaySeq RetryDelays
) : IUpdateDelayer
{
    private static readonly ConcurrentDictionary<TimeSpan, FixedDelayer> Cache = new();

    public static FixedDelayer ZeroUnsafe { get; set; } = new(TimeSpan.Zero);
    public static FixedDelayer Instant { get; set; } = Get(Defaults.MinDelay);

    public FixedDelayer(double updateDelay)
        : this(TimeSpan.FromSeconds(updateDelay), Defaults.RetryDelays) { }
    public FixedDelayer(double updateDelay, RetryDelaySeq retryDelays)
        : this(TimeSpan.FromSeconds(updateDelay), retryDelays) { }
    public FixedDelayer(TimeSpan updateDelay)
        : this(updateDelay, Defaults.RetryDelays) { }

    public static FixedDelayer Get(double updateDelay)
        => Get(TimeSpan.FromSeconds(updateDelay));
    public static FixedDelayer Get(TimeSpan updateDelay)
        => Cache.GetOrAdd(TimeSpanExt.Max(updateDelay, Defaults.MinDelay), static d => new FixedDelayer(d));

    public async ValueTask Delay(int retryCount, CancellationToken cancellationToken = default)
    {
        var delay = TimeSpanExt.Max(UpdateDelay, GetDelay(retryCount));
        if (delay <= TimeSpan.Zero)
            return; // This may only happen if MinDelay == 0 - e.g. for UpdateDelayer.ZeroUnsafe

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    public TimeSpan GetDelay(int retryCount)
        => retryCount > 0 ? RetryDelays[retryCount] : UpdateDelay;

    // Nested types

    public static class Defaults
    {
        private static TimeSpan _minDelay = TimeSpan.FromMilliseconds(32); // ~= 1/30 sec.
        private static RetryDelaySeq _retryDelays = new(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
        private static IMomentClock _clock = MomentClockSet.Default.CpuClock;

        public static TimeSpan MinDelay {
            get => _minDelay;
            set {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _minDelay = value;
                Thread.MemoryBarrier();
                Cache.Clear();
            }
        }

        public static RetryDelaySeq RetryDelays {
            get => _retryDelays;
            set {
                _retryDelays = value;
                Thread.MemoryBarrier();
                Cache.Clear();
            }
        }

        public static IMomentClock Clock {
            get => _clock;
            set {
                _clock = value;
                Thread.MemoryBarrier();
                Cache.Clear();
            }
        }
    }
}
