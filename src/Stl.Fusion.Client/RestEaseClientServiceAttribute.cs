using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RestEaseClientServiceAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }

        public RestEaseClientServiceAttribute(Type? serviceType = null)
            => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddFusion().AddRestEaseClient().AddClientService(ServiceType ?? implementationType, implementationType);
    }
}
