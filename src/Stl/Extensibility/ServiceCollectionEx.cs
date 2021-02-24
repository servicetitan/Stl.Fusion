using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility
{
    public static class ServiceCollectionEx
    {
        public static ModuleBuilder UseModules(this IServiceCollection services)
        {
            var existingDescriptor = services
                .SingleOrDefault(d =>
                    d.ImplementationType == typeof(ModuleBuilder)
                    && d.ImplementationInstance != null
                    && d.Lifetime == ServiceLifetime.Singleton);
            if (existingDescriptor != null)
                return (ModuleBuilder) existingDescriptor.ImplementationInstance!;

            var moduleBuilder = new ModuleBuilder(services);
            services.AddSingleton(moduleBuilder);
            return moduleBuilder;
        }

        public static IServiceCollection UseModules(this IServiceCollection services,
            Action<ModuleBuilder> configureModules)
        {
            var moduleBuilder = services.UseModules();
            configureModules.Invoke(moduleBuilder);
            moduleBuilder.Use();
            return services;
        }
    }
}
