using Stl.Rpc;

namespace Stl.Fusion;

public static class ServiceCollectionExt
{
    public static FusionBuilder AddFusion(
        this IServiceCollection services,
        RpcServiceMode serviceMode = default)
        => new(services, null, serviceMode);

    public static IServiceCollection AddFusion(
        this IServiceCollection services,
        Action<FusionBuilder> configure)
        => new FusionBuilder(services, configure).Services;

    public static IServiceCollection AddFusion(
        this IServiceCollection services,
        RpcServiceMode serviceMode,
        Action<FusionBuilder> configure)
        => new FusionBuilder(services, configure, serviceMode).Services;
}
