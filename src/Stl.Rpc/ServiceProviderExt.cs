namespace Stl.Rpc;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RpcHub RpcHub(this IServiceProvider services)
        => services.GetRequiredService<RpcHub>();
}
