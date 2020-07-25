using System;

namespace Stl.Time
{
    public static class TimeSpanEx
    {
        public static TimeSpan NonNegative(this TimeSpan value)
            => value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }
}
