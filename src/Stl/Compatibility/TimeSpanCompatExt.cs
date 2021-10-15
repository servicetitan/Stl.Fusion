#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System;

public static class TimeSpanCompatExt
{
    public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        => IntervalFromDoubleTicks(Math.Round(timeSpan.Ticks * factor));

    public static TimeSpan Divide(this TimeSpan timeSpan, double divisor)
        => IntervalFromDoubleTicks(Math.Round(timeSpan.Ticks / divisor));

    public static double Divide(this TimeSpan dividend, TimeSpan divisor)
        => dividend.Ticks / (double) divisor.Ticks;

    // Private methods

    private static TimeSpan IntervalFromDoubleTicks(double ticks)
    {
        if (ticks > long.MaxValue || ticks < long.MinValue || double.IsNaN(ticks))
            throw new OverflowException("TimeSpan is too long.");
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return ticks == long.MaxValue ? TimeSpan.MaxValue : new TimeSpan((long) ticks);
    }
}

#endif
