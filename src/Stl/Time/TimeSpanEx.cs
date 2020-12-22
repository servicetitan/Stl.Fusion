using System;

namespace Stl.Time
{
    public static class TimeSpanEx
    {
        public static TimeSpan NonNegative(this TimeSpan value)
            => Max(default, value);
        public static TimeSpan Min(TimeSpan first, TimeSpan second)
            => first < second ? first : second;
        public static TimeSpan Max(TimeSpan first, TimeSpan second)
            => first > second ? first : second;
    }
}
