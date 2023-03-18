namespace Stl.Fusion;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IStateFactory StateFactory(this IServiceProvider services)
        => services.GetRequiredService<IStateFactory>();
}
