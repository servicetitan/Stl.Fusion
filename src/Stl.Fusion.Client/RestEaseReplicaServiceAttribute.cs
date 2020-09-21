using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RestEaseReplicaServiceAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }

        public RestEaseReplicaServiceAttribute(Type? serviceType = null)
            => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddFusion().AddRestEaseClient().AddReplicaService(ServiceType ?? implementationType, implementationType);
    }
}
