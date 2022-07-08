namespace Stl.Fusion;

public static class ServiceCollectionExt
{
    public static FusionBuilder AddFusion(this IServiceCollection services)
        => new(services, null);

    public static IServiceCollection AddFusion(this IServiceCollection services, Action<FusionBuilder> configure) 
        => new FusionBuilder(services, configure).Services;
}
