using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Extensibility;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;

namespace Stl.Plugins
{
    public interface IPluginHostBuilder
    {
        HashSet<Type> PluginTypes { get; set; }
        PluginSetInfo Plugins { get; set; }
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
        public PluginSetInfo Plugins { get; set; } = PluginSetInfo.Empty;
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

            if (!Services.HasService<PluginSetInfo>()) {
                var hasPluginConfiguration = Plugins.InfoByType.Count != 0;
                if (hasPluginConfiguration) {
                    // Plugin set is known, so no need to look for plugins
                    if (PluginTypes.Count > 0)
                        throw Errors.CantUsePluginsTogetherWithPluginTypes();
                    Services.AddSingleton(Plugins);
                }
                else {
                    // Plugin set isn't known, so we need IPluginFinder
                    if (!Services.HasService<IPluginFinder>())
                        Services.AddSingleton<IPluginFinder, PluginFinder>();
                    Services.AddSingleton(services => {
                        var pluginFinder = services.GetService<IPluginFinder>();
                        var plugins = pluginFinder.FindPlugins();
                        return plugins;
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
