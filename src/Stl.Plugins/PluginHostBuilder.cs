using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Stl.Plugins.Internal;

namespace Stl.Plugins
{
    public class PluginHostBuilder
    {
        protected static readonly ConfigurationRoot EmptyConfiguration =
            new(new List<IConfigurationProvider>());

        public IServiceCollection Services { get; set; }
        public Func<IServiceCollection, IServiceProvider> ServiceProviderFactory { get; set; } =
            services => new DefaultServiceProviderFactory().CreateServiceProvider(services);

        public PluginHostBuilder(IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();
            Services = services;
            Services.AddLogging(logging => logging.ClearProviders());

            // Own services
            Services.TryAddSingleton<IPluginHost, PluginHost>();
            Services.TryAddSingleton<IPluginFactory, PluginFactory>();
            Services.TryAddSingleton<IPluginCache, PluginCache>();
            services.TryAddSingleton<IPluginInfoProvider, PluginInfoProvider>();
            Services.TryAddSingleton(typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>));
            Services.TryAddSingleton(typeof(IPluginHandle<>), typeof(PluginHandle<>));
            Services.TryAddSingleton(services1 => {
                var pluginFinder = services1.GetRequiredService<IPluginFinder>();
                return pluginFinder.FoundPlugins
                    ?? throw Errors.PluginFinderRunFailed(pluginFinder.GetType());
            });

            // FileSystemPluginFinder is the default IPluginFinder
            Services.TryAddSingleton<IPluginFinder, FileSystemPluginFinder>();
            Services.TryAddSingleton<FileSystemPluginFinder.Options>();
        }

        public IPluginHost Build()
            => Task.Run(() => BuildAsync()).Result;

        public virtual async Task<IPluginHost> BuildAsync(CancellationToken cancellationToken = default)
        {
            var services = ServiceProviderFactory(Services);
            var pluginFinder = services.GetRequiredService<IPluginFinder>();
            await pluginFinder.Run(cancellationToken).ConfigureAwait(false);
            var host = services.GetRequiredService<IPluginHost>();
            return host;
        }
    }
}
