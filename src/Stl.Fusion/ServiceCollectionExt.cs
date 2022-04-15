namespace Stl.Fusion;

public static class ServiceCollectionExt
{
    public static FusionBuilder AddFusion(this IServiceCollection services)
        => new(services);

    public static IServiceCollection AddFusion(this IServiceCollection services, Action<FusionBuilder> configureFusion)
    {
        var fusion = services.AddFusion();
        configureFusion(fusion);
        return services;
    }
}
