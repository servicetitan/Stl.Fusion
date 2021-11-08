using Microsoft.Extensions.DependencyInjection;

namespace Stl.Time;

public static class ServiceProviderExt
{
    public static MomentClockSet Clocks(this IServiceProvider services)
        => services.GetService<MomentClockSet>() ?? MomentClockSet.Default;

    public static IMomentClock SystemClock(this IServiceProvider services)
        => services.Clocks().SystemClock;
}
