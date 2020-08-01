using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedServiceAttribute : ServiceImplementationAttributeBase
    {
        public ComputedServiceAttribute(Type? serviceType = null) : base(serviceType) { }

        public override void TryRegister(IServiceCollection services, Type implementationType)
            => services.AddComputedService(ServiceType ?? implementationType, implementationType);
    }
}
