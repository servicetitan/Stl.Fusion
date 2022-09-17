namespace Stl.Time;

public static class TimeSpanExt
{
    public static readonly TimeSpan Infinite = TimeSpan.MaxValue;
    public static readonly double InfiniteInSeconds = Infinite.TotalSeconds;

    public static TimeSpan Positive(this TimeSpan value)
        => Max(default, value);
    public static TimeSpan Clamp(this TimeSpan value, TimeSpan min, TimeSpan max)
        => Min(max, Max(min, value));
    public static TimeSpan Min(TimeSpan first, TimeSpan second)
        => new(Math.Min(first.Ticks, second.Ticks));
    public static TimeSpan Max(TimeSpan first, TimeSpan second)
        => new(Math.Max(first.Ticks, second.Ticks));

    public static RandomTimeSpan ToRandom(this TimeSpan value, TimeSpan maxDelta)
        => new(value, maxDelta);
    public static RandomTimeSpan ToRandom(this TimeSpan value, double maxDelta)
        => new(value, maxDelta);
    public static RetryDelaySeq ToRetryDelaySeq(this TimeSpan min, TimeSpan max)
        => new(min, max);

    public static string ToShortString(this TimeSpan value)
    {
        if (value < TimeSpan.FromMilliseconds(0.001))
            return $"{value.TotalMilliseconds * 1000:N3}μs";
        if (value < TimeSpan.FromSeconds(1))
            return $"{value.TotalMilliseconds:N3}ms";
        if (value < TimeSpan.FromSeconds(60))
            return $"{value.TotalSeconds:N3}s";
        if (value < TimeSpan.FromMinutes(60))
            return $"{value.TotalMinutes:N0}m {value.Seconds:N3}s";
        return $"{value.TotalHours:N0}h {value.Minutes:N0}m {value.Seconds:N3}s";
    } 
}
