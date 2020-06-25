using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
        IConfiguration Configuration { get; set; }
        IServiceCollection Services { get; set; }
        Func<IServiceCollection, IServiceProvider> ServiceProviderFactory { get; set; }
        bool AutoStart { get; set; }
        IPluginHostBuilderImpl Implementation { get; }

        IPluginHost Build();
    }

    // This interface is used to hide the methods with
    // similar names exposed via PluginHostBuilderEx 
    public interface IPluginHostBuilderImpl
    {
        IConfigurationBuilder CreateConfigurationBuilder();
        void UseDefaultServices();
    }

    public class PluginHostBuilder : IPluginHostBuilder, IPluginHostBuilderImpl
    {
        protected static readonly ConfigurationRoot EmptyConfiguration = 
            new ConfigurationRoot(new List<IConfigurationProvider>());

        public HashSet<Type> PluginTypes { get; set; } = new HashSet<Type>();
        public PluginSetInfo Plugins { get; set; } = PluginSetInfo.Empty;
        public IServiceCollection Services { get; set; } = new ServiceCollection();
        public IConfiguration Configuration { get; set; } = EmptyConfiguration;
        public Func<IServiceCollection, IServiceProvider> ServiceProviderFactory { get; set; } = 
            services => new DefaultServiceProviderFactory().CreateServiceProvider(services);
        public bool AutoStart { get; set; } = true;
        public IPluginHostBuilderImpl Implementation => this;
        protected IPluginHost? Host { get; set; }

        IConfigurationBuilder IPluginHostBuilderImpl.CreateConfigurationBuilder() => CreateConfigurationBuilder();
        protected IConfigurationBuilder CreateConfigurationBuilder() => new ConfigurationBuilder();

        void IPluginHostBuilderImpl.UseDefaultServices() => UseDefaultServices();
        protected virtual void UseDefaultServices()
        {
            // IPluginHost acts as a tagging service here indicating UseDefaultServices
            // was already called earlier
            if (Services.HasService<IPluginHost>())
                return;

            Services.AddSingleton(_ => Host);
            Services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
            Services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

            if (!Services.HasService<PluginSetInfo>()) {
                var hasPluginSetInfo = Plugins.InfoByType.Count != 0;
                if (hasPluginSetInfo) {
                    // Plugin set is known, so no need to look for plugins
                    Services.TryAddSingleton(Plugins);
                }
                else {
                    // Plugin set isn't known, so we need IPluginFinder
                    Services.TryAddSingleton<IPluginFinder, PluginFinder>();
                    Services.TryAddSingleton(services => {
                        var pluginFinder = services.GetRequiredService<IPluginFinder>();
                        var plugins = pluginFinder.FindPlugins();
                        return plugins;
                    });
                }
            }
            // Adding filter that makes sure only plugins castable to the
            // requested ones are exposed.
            if (PluginTypes.Count > 0) {
                var hPluginTypes = PluginTypes.Select(t => (TypeRef) t).ToHashSet();
                this.AddPluginFilter(p => p.CastableTo.Any(c => hPluginTypes.Contains(c)));
            }

            Services.TryAddSingleton<IPluginFactory, PluginFactory>();
            Services.TryAddSingleton<IPluginCache, PluginCache>();
            Services.TryAddSingleton(typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>));
            Services.TryAddSingleton(typeof(IPluginHandle<>), typeof(PluginHandle<>));
        }

        public virtual IPluginHost Build()
        {
            if (Host != null)
                throw Errors.AlreadyInvoked(nameof(Build));
            UseDefaultServices();
            var plugins = ServiceProviderFactory(Services);
            Host = new PluginHost(plugins);
            if (AutoStart)
                Host.Start();
            return Host;
        }
    }
}
