namespace Stl.Fusion.UI;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIActionTracker UIActionTracker(this IServiceProvider services)
        => services.GetRequiredService<UIActionTracker>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UICommander UICommander(this IServiceProvider services)
        => services.GetRequiredService<UICommander>();
}
