using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.RegisterAttributes;

namespace Stl.Fusion.Client
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RegisterRestEaseReplicaServiceAttribute : RegisterAttribute
    {
        public Type? ServiceType { get; set; }
        public bool IsCommandService { get; set; } = true;

        public RegisterRestEaseReplicaServiceAttribute(Type? serviceType = null)
            => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services
                .AddFusion()
                .AddRestEaseClient()
                .AddReplicaService(
                    ServiceType ?? implementationType, implementationType,
                    isCommandService: IsCommandService);
    }
}
