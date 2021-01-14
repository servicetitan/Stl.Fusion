using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public class CommandServiceAttribute : ServiceAttribute
    {
        public double? OrderOverride { get; set; }

        public CommandServiceAttribute(Type? serviceType = null) : base(serviceType) { }

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddCommander().AddCommandService(
                ServiceType ?? implementationType, implementationType, Lifetime, OrderOverride);
    }
}
