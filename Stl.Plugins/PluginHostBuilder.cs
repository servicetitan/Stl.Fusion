using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Extensibility;
using Stl.Plugins.Internal;
using Stl.Reflection;

namespace Stl.Plugins
{
    public interface IPluginHostBuilder
    {
        IPluginConfiguration PluginConfiguration { get; set; }
        HashSet<Type> PluginTypes { get; set; }
        IServiceCollection Services { get; set; }
        IPluginHostBuilderImpl Implementation { get; }

        IServiceProvider Build();
    }

    // This interface is used to hide the methods with
    // similar names exposed via PluginHostBuilderEx 
    public interface IPluginHostBuilderImpl
    {
        void UseDefaultServices();
    }

    public class PluginHostBuilder : IPluginHostBuilder, IPluginHostBuilderImpl
    {
        public HashSet<Type> PluginTypes { get; set; } = new HashSet<Type>();
        public IPluginConfiguration PluginConfiguration { get; set; } = Plugins.PluginConfiguration.Empty;
        public IServiceCollection Services { get; set; } = new ServiceCollection();
        public IPluginHostBuilderImpl Implementation => this;

        void IPluginHostBuilderImpl.UseDefaultServices() => UseDefaultServices();
        protected virtual void UseDefaultServices()
        {
            if (Services.HasService<IPluginHostBuilder>())
                return;
            // It's a tagging service indicating Configure was called earlier
            Services.AddSingleton<IPluginHostBuilder>(_ => null!);
            
            if (!Services.HasService<ILoggerFactory>())
                Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            if (!Services.HasService(typeof(ILogger<>)))
                Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            if (!Services.HasService<IPluginConfiguration>()) {
                var hasPluginConfiguration = PluginConfiguration.Interfaces.Length != 0;
                if (hasPluginConfiguration) {
                    // Plugin configuration is known, so no need to look for plugins
                    if (PluginTypes.Count > 0)
                        throw Errors.CantUsePluginConfigurationWithPluginTypes();
                    Services.AddSingleton(PluginConfiguration);
                }
                else {
                    // Plugin configuration isn't known, so we need IPluginFinder
                    if (!Services.HasService<IPluginFinder>())
                        Services.AddSingleton<IPluginFinder, PluginFinder>();
                    Services.AddSingleton<IPluginConfiguration>(services => {
                        var pluginFinder = services.GetService<IPluginFinder>();
                        var pluginSetInfo = pluginFinder.FindPlugins();
                        var pluginTypes = PluginTypes.Select(t => (TypeRef) t).ToArray();
                        return new PluginConfiguration(pluginSetInfo, pluginTypes);
                    });
                }
            }
            if (!Services.HasService<IPluginFactory>())
                Services.AddSingleton<IPluginFactory, PluginFactory>();
            if (!Services.HasService<IPluginCache>())
                Services.AddSingleton<IPluginCache, PluginCache>();
            if (!Services.HasService(typeof(IPluginInstanceHandle<>)))
                Services.AddSingleton(typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>));
            if (!Services.HasService(typeof(IPluginHandle<>)))
                Services.AddSingleton(typeof(IPluginHandle<>), typeof(PluginHandle<>));
        }

        public virtual IServiceProvider Build()
        {
            UseDefaultServices();
            var factory = new DefaultServiceProviderFactory();
            return factory.CreateServiceProvider(Services);
        }
    }
}
