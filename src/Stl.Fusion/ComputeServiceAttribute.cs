using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputeServiceAttribute : ServiceAttribute
    {
        public ComputeServiceAttribute(Type? serviceType = null) : base(serviceType) { }

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddFusion().AddComputeService(
                ServiceType ?? implementationType, implementationType, Lifetime);
    }
}
