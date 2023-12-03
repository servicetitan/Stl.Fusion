namespace Stl.Time;

public static class MomentExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Moment? NullIfDefault(this Moment moment)
        => moment == default ? null : moment;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Moment DefaultIfNull(this Moment? moment)
        => moment is { } vMoment ? vMoment : default;
}
