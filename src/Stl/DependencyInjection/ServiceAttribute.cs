using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.DependencyInjection
{
    public class ServiceAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
        public bool IsEnumerable { get; set; }

        public ServiceAttribute(Type? serviceType = null)
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
}
