using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;

namespace Stl.Plugins
{
    public interface IPluginContainerBuilder
    {
        PluginContainerConfiguration Configuration { get; set; }
        void ConfigureServices(IServiceCollection services);
        IServiceProvider BuildContainer();
    }

    public class PluginContainerBuilder : IPluginContainerBuilder
    {
        public PluginContainerConfiguration Configuration { get; set; } = 
            PluginContainerConfiguration.Empty;

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPluginContainerConfiguration>(Configuration);
            services.AddSingleton<IPluginFactory, PluginFactory>();
            services.AddSingleton<IPluginCache, PluginCache>();
            services.AddSingleton(typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>));
            services.AddSingleton(typeof(IPluginHandle<>), typeof(PluginHandle<>));
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
