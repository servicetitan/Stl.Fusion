namespace Stl.RestEase;

public static class ServiceCollectionExt
{
    public static RestEaseBuilder AddRestEase(this IServiceCollection services)
        => new(services, null);

    public static IServiceCollection AddRestEase(this IServiceCollection services, Action<RestEaseBuilder> configure)
        => new RestEaseBuilder(services, configure).Services;
}
