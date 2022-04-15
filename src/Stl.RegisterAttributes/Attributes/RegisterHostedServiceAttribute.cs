using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Stl.RegisterAttributes.Internal;

namespace Stl.RegisterAttributes;

public class RegisterHostedServiceAttribute : RegisterServiceAttribute
{
    public override void Register(IServiceCollection services, Type implementationType)
    {
        if (Lifetime != ServiceLifetime.Singleton)
            throw Errors.HostedServiceHasToBeSingleton(implementationType);
        var serviceType = ServiceType ?? implementationType;

        // The code is tricky because TryAddEnumerable requires that
        // passed factory delegate (Func<...>) indicates correct implementation
        // type in its last type argument.
        var factory = (Func<IServiceProvider, object>) CreateServiceFactoryMethod
            .MakeGenericMethod(serviceType)
            .Invoke(null, Array.Empty<object>())!;
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(
            typeof(IHostedService), factory));
    }

    private static MethodInfo CreateServiceFactoryMethod = typeof(RegisterHostedServiceAttribute)
        .GetMethod(nameof(CreateServiceFactory), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static Func<IServiceProvider, TImpl> CreateServiceFactory<TImpl>()
        where TImpl : class
        => c => c.GetRequiredService<TImpl>();
}
