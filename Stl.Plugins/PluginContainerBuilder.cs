using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins
{
    public interface IPluginContainerBuilder
    {
        PluginSetInfo PluginSetInfo { get; }
        void ConfigureServices(IServiceCollection services);
        IServiceProvider BuildContainer();
    }

    public class PluginContainerBuilder : IPluginContainerBuilder
    {
        public PluginSetInfo PluginSetInfo { get; }

        public PluginContainerBuilder(PluginSetInfo pluginSetInfo) 
            => PluginSetInfo = pluginSetInfo;

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(PluginSetInfo);
            services.AddSingleton<IPluginCache, PluginCache>();
        }

        public virtual IServiceProvider BuildContainer()
        {
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(services);
            var factory = new DefaultServiceProviderFactory();
            return factory.CreateServiceProvider(services);
        }
    }
}
