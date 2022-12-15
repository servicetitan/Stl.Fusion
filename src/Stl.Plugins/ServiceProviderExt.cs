namespace Stl.Plugins;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IPluginHost Plugins(this IServiceProvider services)
        => services.GetRequiredService<IPluginHost>();
}
