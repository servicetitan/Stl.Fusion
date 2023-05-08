namespace Stl.Rpc;

public static class ServiceProviderExt
{
    public static RpcHub RpcHub(this IServiceProvider services)
        => services.GetRequiredService<RpcHub>();
}
