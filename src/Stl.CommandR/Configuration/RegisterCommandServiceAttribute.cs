using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public class RegisterCommandServiceAttribute : RegisterServiceAttribute
    {
        public double? PriorityOverride { get; set; }

        public RegisterCommandServiceAttribute(Type? serviceType = null)
            : base(serviceType) { }

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddCommander().AddCommandService(
                ServiceType ?? implementationType, implementationType, Lifetime, PriorityOverride);
    }
}
