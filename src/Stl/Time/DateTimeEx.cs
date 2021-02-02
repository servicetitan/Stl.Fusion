using System;

namespace Stl.Time
{
    public static class DateTimeEx
    {
        public static Moment ToMoment(this DateTime source)
            => new(source);
        public static Moment? ToMoment(this DateTime? source)
            => source.HasValue ? new Moment(source.GetValueOrDefault()) : null;

        public static Moment ToMoment(this DateTimeOffset source)
            => new(source);
        public static Moment? ToMoment(this DateTimeOffset? source)
            => source.HasValue ? new Moment(source.GetValueOrDefault()) : null;

        public static DateTime DefaultKind(this DateTime source, DateTimeKind kind)
            => source.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(source, kind)
                : source;
        public static DateTime? DefaultKind(this DateTime? source, DateTimeKind kind)
            => source.HasValue
                ? (source.GetValueOrDefault().Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(source.GetValueOrDefault(), kind)
                    : source)
                : null;
    }
}
