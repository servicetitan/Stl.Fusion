using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.DependencyInjection
{
    [Serializable]
    public class ServiceAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

        public ServiceAttribute(Type? serviceType = null)
            => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
        {
            var descriptor = new ServiceDescriptor(
                ServiceType ?? implementationType, implementationType, Lifetime);
            services.TryAdd(descriptor);
        }
    }
}
