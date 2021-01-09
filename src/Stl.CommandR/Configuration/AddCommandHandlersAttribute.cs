using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public class AddCommandHandlersAttribute : ServiceAttributeBase
    {
        public Type? ServiceType { get; set; }
        public double? PriorityOverride { get; set; }

        public AddCommandHandlersAttribute() { }
        public AddCommandHandlersAttribute(Type serviceType) => ServiceType = serviceType;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddCommander()
                .AddHandlers(ServiceType ?? implementationType, PriorityOverride);
    }
}
