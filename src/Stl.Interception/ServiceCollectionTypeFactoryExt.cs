using Stl.Interception.Interceptors;

namespace Stl.Interception;

public static class ServiceCollectionTypeFactoryExt
{
    public static IServiceCollection AddScopedTypeFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
    {
        return AddTypeFactory<TFactory>(services, ServiceLifetime.Scoped);
    }

    public static IServiceCollection AddSingletonTypeFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
    {
        return AddTypeFactory<TFactory>(services, ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddTypeFactory<TFactory>(this IServiceCollection services, ServiceLifetime lifetime)
        where TFactory : class, IRequiresAsyncProxy
    {
        return AddTypeFactory<TFactory, TypeFactoryInterceptor>(services, lifetime);
    }

    public static IServiceCollection AddTypeFactory<TFactory, TInterceptor>(this IServiceCollection services, ServiceLifetime lifetime)
        where TFactory : class, IRequiresAsyncProxy
        where TInterceptor : Interceptor
    {
        return AddTypeFactory(services, typeof(TFactory), typeof(TInterceptor), lifetime);
    }

    public static IServiceCollection AddTypeFactory(this IServiceCollection services, Type factoryType, Type interceptorType, ServiceLifetime lifetime)
    {
        // All implementations converge here.
        // Single source of Proxies.GetProxyType if not specified by any of the TProxy-variants.
        return AddTypeFactory(services, factoryType, Proxies.GetProxyType(factoryType), interceptorType, lifetime);
    }

    public static IServiceCollection AddTypeFactory<TFactory, TProxy, TInterceptor>(this IServiceCollection services, ServiceLifetime lifetime)
        where TFactory : class, IRequiresAsyncProxy
        where TProxy : TFactory
        where TInterceptor : Interceptor
    {
        return AddTypeFactory(services, typeof(TFactory), typeof(TProxy), typeof(TInterceptor), lifetime);
    }

    public static IServiceCollection AddTypeFactory(
        this IServiceCollection services,
        Type factoryType,
        Type proxyType,
        Type interceptorType,
        ServiceLifetime lifetime)
    {
        services.Add(new ServiceDescriptor(factoryType, services =>
        { // no way to circumvent closure here
            var interceptor = (Interceptor)services.GetOrActivate(interceptorType);
            var proxy = (IProxy)services.GetOrActivate(proxyType);
            interceptor.BindTo(proxy);
            return proxy;
        }, lifetime));
        return services;
    }

    public static IServiceCollection UseTypeFactories(this IServiceCollection services)
    {
        services.AddScoped<TypeFactoryInterceptor>();
        return services;
    }
}
