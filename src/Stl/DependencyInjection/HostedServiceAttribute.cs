using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.DependencyInjection
{
    public class HostedServiceAttribute : ServiceAttribute
    {
        public bool RegisterService { get; set; } = false;

        public override void Register(IServiceCollection services, Type implementationType)
        {
            if (Lifetime != ServiceLifetime.Singleton)
                throw Errors.HostedServiceHasToBeSingleton(implementationType);
            var serviceType = ServiceType ?? implementationType;
            if (RegisterService) {
                base.Register(services, implementationType);
                services.TryAddEnumerable(ServiceDescriptor.Singleton(
                    typeof(IHostedService), c => c.GetRequiredService(serviceType)));
            }
            else {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(
                    typeof(IHostedService), implementationType));
            }
        }
    }
}
