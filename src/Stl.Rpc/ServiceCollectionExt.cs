namespace Stl.Rpc;

public static class ServiceCollectionExt
{
    public static RpcBuilder AddRpc(this IServiceCollection services)
        => new(services, null);

    public static IServiceCollection AddRpc(this IServiceCollection services, Action<RpcBuilder> configure)
        => new RpcBuilder(services, configure).Services;
}
