using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Text;

namespace Stl.Extensibility
{
    public class RegisterModuleAttribute : RegisterAttribute
    {
        public static Symbol DefaultScope { get; } = "Module";

        public RegisterModuleAttribute()
        {
            // Let's make sure modules don't auto-register together
            // with regular services: most likely this isn't intentional.
            Scope = DefaultScope.Value;
        }

        // This method registers module in ModuleBuilder.ModuleBuilderServices
        public override void Register(IServiceCollection services, Type implementationType)
            => services.TryAddEnumerable(
                ServiceDescriptor.Singleton(typeof(IModule), implementationType));
    }
}
