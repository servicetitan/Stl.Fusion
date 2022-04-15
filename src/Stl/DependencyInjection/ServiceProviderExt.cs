namespace Stl.DependencyInjection;

public static class ServiceProviderExt
{
    public static IServiceProvider Empty { get; } = new ServiceCollection().BuildServiceProvider();

    // Logging extensions

    public static ILoggerFactory Logs(this IServiceProvider services)
    {
        try {
            return services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        }
        catch (ObjectDisposedException) {
            // ILoggerFactory could be requested during IServiceProvider disposal,
            // and we don't want to get any exceptions during this process.
            return NullLoggerFactory.Instance;
        }
    }

    public static ILogger LogFor<T>(this IServiceProvider services)
        => services.LogFor(typeof(T));
    public static ILogger LogFor(this IServiceProvider services, Type type)
        => services.Logs().CreateLogger(type);
    public static ILogger LogFor(this IServiceProvider services, string category)
        => services.Logs().CreateLogger(category);

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
