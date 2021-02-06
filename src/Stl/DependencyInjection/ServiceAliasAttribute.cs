using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.DependencyInjection
{
    public class ServiceAliasAttribute : ServiceAttributeBase
    {
        public Type ServiceType { get; set; }
        public Type? ActualServiceType { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

        public ServiceAliasAttribute(Type serviceType, Type? actualServiceType = null)
        {
            ServiceType = serviceType;
            ActualServiceType = actualServiceType;
        }

        public override void Register(IServiceCollection services, Type implementationType)
        {
            var descriptor = new ServiceDescriptor(
                ServiceType,
                c => c.GetRequiredService(ActualServiceType ?? implementationType),
                Lifetime);
            services.TryAdd(descriptor);
        }
    }
}
