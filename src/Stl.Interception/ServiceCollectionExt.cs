using Stl.Interception.Interceptors;

namespace Stl.Interception;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddTypeViewFactory(
        this IServiceCollection services,
        Func<IServiceProvider, TypeViewProxyGenerator.Options>? optionsFactory = null)
    {
        services.AddSingleton<TypeViewInterceptor>();
        services.AddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        services.AddSingleton<ITypeViewProxyGenerator, TypeViewProxyGenerator>();
        services.AddSingleton<ITypeViewFactory, TypeViewFactory>();
        return services;
    }
}
