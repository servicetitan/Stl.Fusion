namespace Stl.CommandR;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICommander Commander(this IServiceProvider services)
        => services.GetRequiredService<ICommander>();

    public static bool IsScoped(this IServiceProvider services)
    {
        services = services.GetRequiredService<IServiceProvider>();
        return !ReferenceEquals(services.Commander().Services, services);
    }
}
