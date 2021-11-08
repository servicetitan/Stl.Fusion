using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection;

public static class ServiceProviderExt
{
    public static IServiceProvider Empty { get; } = new ServiceCollection().BuildServiceProvider();

    // Get HostedServiceManager

    public static HostedServiceGroupManager HostedServices(this IServiceProvider services)
        => new(services);

    // GetOrActivate

    public static T GetOrActivate<T>(this IServiceProvider services, params object[] arguments)
        => (T) services.GetOrActivate(typeof(T));

    public static object GetOrActivate(this IServiceProvider services, Type type, params object[] arguments)
        => services.GetService(type) ?? services.Activate(type);

    // Activate

    public static T Activate<T>(this IServiceProvider services, params object[] arguments)
        => (T) services.Activate(typeof(T), arguments);

    public static object Activate(this IServiceProvider services, Type instanceType, params object[] arguments)
        => ActivatorUtilities.CreateInstance(services, instanceType, arguments);
}
