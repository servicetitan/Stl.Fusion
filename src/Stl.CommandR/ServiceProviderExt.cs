namespace Stl.CommandR;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICommander Commander(this IServiceProvider services)
        => services.GetRequiredService<ICommander>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScoped(this IServiceProvider services)
        => !ReferenceEquals(services.Commander().Services, services);
}
