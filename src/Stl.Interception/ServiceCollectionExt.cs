using Stl.Interception.Interceptors;

namespace Stl.Interception;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddTypeViewFactory(this IServiceCollection services)
    {
        services.AddSingleton<TypeViewInterceptor>();
        services.AddSingleton<ITypeViewFactory, TypeViewFactory>();
        return services;
    }
}
