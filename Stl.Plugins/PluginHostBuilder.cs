using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Extensibility;
using Stl.Internal;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;
using Stl.Reflection;

namespace Stl.Plugins
{
    public interface IPluginHostBuilder
    {
        HashSet<Type> PluginTypes { get; set; }
        PluginSetInfo Plugins { get; set; }
        IServiceCollection Services { get; set; }
        bool AutoStart { get; set; }
        IPluginHostBuilderImpl Implementation { get; }

        IPluginHost Build();
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
        public bool AutoStart { get; set; } = true;
        public IPluginHostBuilderImpl Implementation => this;
        protected IPluginHost? Host { get; set; }

        void IPluginHostBuilderImpl.UseDefaultServices() => UseDefaultServices();
        protected virtual void UseDefaultServices()
        {
            // IPluginHost acts as a tagging service here indicating UseDefaultServices
            // was already called earlier
            if (Services.HasService<IPluginHost>())
                return;

            Services.AddSingleton(_ => Host);
            Services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>());
            Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

            if (!Services.HasService<PluginSetInfo>()) {
                var hasPluginSetInfo = Plugins.InfoByType.Count != 0;
                if (hasPluginSetInfo) {
                    // Plugin set is known, so no need to look for plugins
                    Services.TryAdd(ServiceDescriptor.Singleton(Plugins));
                }
                else {
                    // Plugin set isn't known, so we need IPluginFinder
                    Services.TryAdd(ServiceDescriptor.Singleton<IPluginFinder, PluginFinder>());
                    Services.TryAdd(ServiceDescriptor.Singleton(services => {
                        var pluginFinder = services.GetService<IPluginFinder>();
                        var plugins = pluginFinder.FindPlugins();
                        return plugins;
                    }));
                }
            }
            // Adding filter that makes sure only plugins castable to the
            // requested ones are exposed.
            if (PluginTypes.Count > 0) {
                var hPluginTypes = PluginTypes.Select(t => (TypeRef) t).ToHashSet();
                this.AddPluginFilter(p => p.CastableTo.Any(c => hPluginTypes.Contains(c)));
            }

            Services.TryAdd(ServiceDescriptor.Singleton<IPluginFactory, PluginFactory>());
            Services.TryAdd(ServiceDescriptor.Singleton<IPluginCache, PluginCache>());
            Services.TryAdd(ServiceDescriptor.Singleton(
                typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>)));
            Services.TryAdd(ServiceDescriptor.Singleton(
                typeof(IPluginHandle<>), typeof(PluginHandle<>)));
        }

        public virtual IPluginHost Build()
        {
            if (Host != null)
                throw Errors.AlreadyInvoked(nameof(Build));
            UseDefaultServices();
            var factory = new DefaultServiceProviderFactory();
            var plugins = factory.CreateServiceProvider(Services);
            Host = new PluginHost(plugins);
            if (AutoStart)
                Host.Start();
            return Host;
        }
    }
}
