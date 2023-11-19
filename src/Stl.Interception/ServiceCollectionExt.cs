using System.Diagnostics.CodeAnalysis;
using Stl.Interception.Interceptors;

namespace Stl.Interception;

public static class ServiceCollectionExt
{
    // TypeViewFactory

    public static IServiceCollection AddTypeViewFactory(this IServiceCollection services)
    {
        services.AddSingleton<TypeViewInterceptor>();
        services.AddSingleton<ITypeViewFactory, TypeViewFactory>();
        return services;
    }

    // Typed factories

    public static IServiceCollection UseTypedFactories(this IServiceCollection services)
        => services.AddScoped<TypedFactoryInterceptor>();

    public static IServiceCollection AddTypedFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TFactory>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TFactory : class, IRequiresAsyncProxy
        => services.AddTypedFactory<TFactory, TypedFactoryInterceptor>(lifetime);

    public static IServiceCollection AddTypedFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TFactory,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInterceptor>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TFactory : class, IRequiresAsyncProxy
        where TInterceptor : Interceptor
        => services.AddTypedFactory(typeof(TFactory), typeof(TInterceptor), lifetime);

    public static IServiceCollection AddTypedFactory(
        this IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type factoryType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => services.AddTypedFactory(factoryType, typeof(TypedFactoryInterceptor), lifetime);

    public static IServiceCollection AddTypedFactory(
        this IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type factoryType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interceptorType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        // All implementations converge here.
        services.Add(new ServiceDescriptor(factoryType, c => {
            var factoryProxyType = Proxies.GetProxyType(factoryType);
            var interceptor = (Interceptor)c.GetOrActivate(interceptorType);
#pragma warning disable IL2072
            var proxy = (IProxy)c.GetOrActivate(factoryProxyType);
#pragma warning restore IL2072
            interceptor.BindTo(proxy);
            return proxy;
        }, lifetime));
        return services;
    }
}
