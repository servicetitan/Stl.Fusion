using Stl.Internal;

namespace Stl.DependencyInjection;

public static class CustomResolverExt
{
    public static object? TryResolve(this CustomResolver? resolver, IServiceProvider services)
    {
        if (resolver == null)
            return null;

        if (resolver.Resolver == null)
            return services.GetService(resolver.Type);

        var service = resolver.Resolver.Invoke(services);
        if (ReferenceEquals(service, null))
            return service;

        var actualType = service.GetType();
        return resolver.Type.IsAssignableFrom(actualType)
            ? service
            : throw Errors.MustBeAssignableTo(actualType, resolver.Type);
    }

    public static object Resolve(this CustomResolver? resolver, IServiceProvider services)
    {
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));

        if (resolver.Resolver == null)
            return services.GetRequiredService(resolver.Type);

        var service = resolver.Resolver.Invoke(services);
        if (ReferenceEquals(service, null))
            throw Errors.ImplementationNotFound(resolver.Type);

        var actualType = service.GetType();
        return resolver.Type.IsAssignableFrom(actualType)
            ? service
            : throw Errors.MustBeAssignableTo(actualType, resolver.Type);
    }
}
