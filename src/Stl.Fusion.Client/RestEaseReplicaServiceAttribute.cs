using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RestEaseReplicaServiceAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }
        public bool AddCommandService { get; set; } = true;

        public RestEaseReplicaServiceAttribute(Type? serviceType = null)
            => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services
                .AddFusion()
                .AddRestEaseClient()
                .AddReplicaService(
                    ServiceType ?? implementationType, implementationType,
                    addCommandService: AddCommandService);
    }
}
