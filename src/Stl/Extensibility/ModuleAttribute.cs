using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Text;

namespace Stl.Extensibility
{
    public class ModuleAttribute : ServiceAttributeBase
    {
        public static Symbol DefaultScope { get; } = "Module";

        public ModuleAttribute()
        {
            // Let's make sure Plugins aren't auto-registered together
            // with regular services: most likely this isn't intentional.
            Scope = DefaultScope.Value;
        }

        // This method registers plugin in ModuleBuilder.ModuleBuilderServices
        public override void Register(IServiceCollection services, Type implementationType)
            => services.TryAddEnumerable(
                ServiceDescriptor.Singleton(typeof(IModule), implementationType));
    }
}
