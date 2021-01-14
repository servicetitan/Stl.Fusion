using System;

namespace Stl.Time
{
    public static class DateTimeEx
    {
        public static Moment ToMoment(this DateTime source) => new(source);
        public static Moment ToMoment(this DateTimeOffset source) => new(source);

        public static DateTime DefaultKind(this DateTime source, DateTimeKind kind)
            => source.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(source, kind)
                : source;
    }
}
