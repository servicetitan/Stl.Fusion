using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.RegisterAttributes;

public class RegisterServiceAttribute : RegisterAttribute
{
    public Type? ServiceType { get; set; }
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    public bool IsEnumerable { get; set; }

    public RegisterServiceAttribute(Type? serviceType = null)
        => ServiceType = serviceType;

    public override void Register(IServiceCollection services, Type implementationType)
    {
        var descriptor = new ServiceDescriptor(
            ServiceType ?? implementationType, implementationType, Lifetime);
        if (IsEnumerable)
            services.TryAddEnumerable(descriptor);
        else
            services.TryAdd(descriptor);
    }
}
