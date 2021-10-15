namespace Stl.Time;

public static class DateTimeExt
{
#if !NETSTANDARD2_0
    public static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
#else
    public static readonly DateTime UnixEpoch = new(621355968000000000L, DateTimeKind.Utc);
#endif

    public static Moment ToMoment(this DateTime source)
        => new(source);
    public static Moment? ToMoment(this DateTime? source)
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
