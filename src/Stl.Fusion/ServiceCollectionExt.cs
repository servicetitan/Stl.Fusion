using Stl.Rpc;

namespace Stl.Fusion;

public static class ServiceCollectionExt
{
    public static FusionBuilder AddFusion(
        this IServiceCollection services,
        RpcServiceMode serviceMode = default,
        bool setDefaultServiceMode = false)
        => new(services, null, serviceMode, setDefaultServiceMode);

    public static IServiceCollection AddFusion(
        this IServiceCollection services,
        Action<FusionBuilder> configure)
        => new FusionBuilder(services, configure, default, false).Services;

    public static IServiceCollection AddFusion(
        this IServiceCollection services,
        RpcServiceMode serviceMode,
        Action<FusionBuilder> configure)
        => new FusionBuilder(services, configure, serviceMode, false).Services;

    public static IServiceCollection AddFusion(
        this IServiceCollection services,
        RpcServiceMode serviceMode,
        bool setDefaultServiceMode,
        Action<FusionBuilder> configure)
        => new FusionBuilder(services, configure, serviceMode, setDefaultServiceMode).Services;
}
