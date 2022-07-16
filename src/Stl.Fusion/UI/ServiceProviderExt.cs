namespace Stl.Fusion.UI;

public static class ServiceProviderExt
{
    public static UIActionTracker UIActionTracker(this IServiceProvider services)
        => services.GetService<UIActionTracker>() ?? UI.UIActionTracker.None;

    public static UICommander UICommander(this IServiceProvider services)
        => services.GetRequiredService<UICommander>();
}
