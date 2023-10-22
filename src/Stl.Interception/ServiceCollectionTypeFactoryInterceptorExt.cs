namespace Stl.Interception;

public static class ServiceCollectionTypeFactoryInterceptorExt
{
    public static IServiceCollection AddScopedTypeFactory<TFactory, TInterceptor>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
        where TInterceptor : Interceptor
    {
        return services.AddTypeFactory<TFactory, TInterceptor>(ServiceLifetime.Scoped);
    }

    public static IServiceCollection AddSingletonTypeFactory<TFactory, TInterceptor>(this IServiceCollection services)
        where TFactory : class, IRequiresAsyncProxy
        where TInterceptor : Interceptor
    {
        return services.AddTypeFactory<TFactory, TInterceptor>(ServiceLifetime.Singleton);
    }
}
