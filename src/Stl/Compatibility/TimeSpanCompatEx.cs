#if NETSTANDARD2_0

namespace System
{
    public static class TimeSpanCompatEx
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
            => IntervalFromDoubleTicks(Math.Round(timeSpan.Ticks * factor));

        public static TimeSpan Divide(this TimeSpan timeSpan, double divisor)
            => IntervalFromDoubleTicks(Math.Round((double) timeSpan.Ticks / divisor));

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
}

#endif
