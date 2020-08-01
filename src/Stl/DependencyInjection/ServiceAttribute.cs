using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.DependencyInjection
{
    [Serializable]
    public class ServiceAttribute : ServiceImplementationAttributeBase
    {
        public ServiceLifetime Lifetime { get; } = ServiceLifetime.Singleton;

        public ServiceAttribute(Type? serviceType = null) : base(serviceType) { }

        public override void TryRegister(IServiceCollection services, Type implementationType)
        {
            var descriptor = new ServiceDescriptor(
                ServiceType ?? implementationType, implementationType, Lifetime);
            services.TryAdd(descriptor);
        }
    }
}
