using System;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Stl.Extensibility.Internal;

namespace Stl.Extensibility
{
    public sealed class ModuleBuilder
    {
        private readonly Lazy<IImmutableSet<IModule>> _modulesLazy;
        private IServiceCollection ModuleServices { get; }

        public IServiceCollection Services { get; }
        public IImmutableSet<IModule> Modules => _modulesLazy.Value;

        internal ModuleBuilder(IServiceCollection services)
        {
            Services = services;
            ModuleServices = new ServiceCollection().AddSingleton(Services);
            _modulesLazy = new Lazy<IImmutableSet<IModule>>(CreateModules);
        }

        public ModuleBuilder ConfigureModuleServices(Action<IServiceCollection>? configureModuleServices)
        {
            if (_modulesLazy.IsValueCreated)
                throw Errors.CannotConfigureModulesOnceTheyAreCreated();
            configureModuleServices?.Invoke(ModuleServices);
            return this;
        }

        public ModuleBuilder Use()
        {
            foreach (var plugin in Modules)
                plugin.Use();
            return this;
        }

        // Protected methods

        private IImmutableSet<IModule> CreateModules()
        {
            var moduleBuilderServices = ModuleServices.BuildServiceProvider();
            return moduleBuilderServices.GetServices<IModule>().ToImmutableHashSet();
        }
    }
}
