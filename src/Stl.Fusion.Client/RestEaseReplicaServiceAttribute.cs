using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RestEaseReplicaServiceAttribute : ServiceImplementationAttributeBase
    {
        public RestEaseReplicaServiceAttribute(Type? serviceType = null) : base(serviceType) { }

        public override void TryRegister(IServiceCollection services, Type implementationType)
            => services.AddRestEaseReplicaService(ServiceType ?? implementationType, implementationType);
    }
}
