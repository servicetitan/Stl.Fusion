using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class AddCommandHandlersAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }
        public double? PriorityOverride { get; set; }

        public AddCommandHandlersAttribute() { }
        public AddCommandHandlersAttribute(Type serviceType) => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddCommandR()
                .AddHandlers(ServiceType ?? implementationType, PriorityOverride);
    }
}
