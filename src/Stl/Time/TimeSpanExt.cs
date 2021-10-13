using System;

namespace Stl.Time
{
    public static class TimeSpanExt
    {
        public static TimeSpan NonNegative(this TimeSpan value)
            => Max(default, value);
        public static TimeSpan Clamp(this TimeSpan value, TimeSpan min, TimeSpan max)
            => Min(max, Max(min, value));
        public static TimeSpan Min(TimeSpan first, TimeSpan second)
            => first < second ? first : second;
        public static TimeSpan Max(TimeSpan first, TimeSpan second)
            => first > second ? first : second;
    }
}
