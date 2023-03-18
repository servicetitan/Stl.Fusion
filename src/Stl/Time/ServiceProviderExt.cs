namespace Stl.Time;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MomentClockSet Clocks(this IServiceProvider services)
        => services.GetService<MomentClockSet>() ?? MomentClockSet.Default;
}
