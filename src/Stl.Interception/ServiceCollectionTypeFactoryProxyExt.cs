using Stl.Interception.Interceptors;

namespace Stl.Interception;

public static class ServiceCollectionTypeFactoryProxyExt
{
    public static IServiceCollection AddScopedTypeFactory<TFactory, TProxy>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
        where TProxy : TFactory
    {
        return AddTypeFactory<TFactory, TProxy>(services, ServiceLifetime.Scoped);
    }

    public static IServiceCollection AddSingletonTypeFactory<TFactory, TProxy>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
        where TProxy : TFactory
    {
        return AddTypeFactory<TFactory, TProxy>(services, ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddTypeFactory<TFactory, TProxy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TFactory : class, IRequiresAsyncProxy
        where TProxy : TFactory
    {
        return services.AddTypeFactory<TFactory, TProxy, TypeFactoryInterceptor>(lifetime);
    }
}
