#if NETSTANDARD2_0

namespace System
{
    public static class TimeSpanEx
    {
        private static TimeSpan IntervalFromDoubleTicks(double ticks)
        {
            if (ticks > long.MaxValue || ticks < long.MinValue || double.IsNaN(ticks))
                throw new OverflowException("TimeSpanTooLong");
            return ticks == (double) long.MaxValue ? TimeSpan.MaxValue : new TimeSpan((long) ticks);
        }
        
        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        {
            if (double.IsNaN(factor))
                throw new ArgumentException("Argument can not be NaN", nameof (factor));
            return IntervalFromDoubleTicks(Math.Round(timeSpan.Ticks * factor));
        }


        public static TimeSpan Divide(this TimeSpan timeSpan, double divisor)
        {
            if (double.IsNaN(divisor))
                throw new ArgumentException("Argument can not be NaN", nameof (divisor));
            return IntervalFromDoubleTicks(Math.Round((double) timeSpan.Ticks / divisor));
        }

        public static double Divide(this TimeSpan t1, TimeSpan t2)
        {
            return (double)t1.Ticks / (double)t2.Ticks;
        }
    }
}

#endif