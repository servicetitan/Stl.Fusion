using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Plugins 
{
    public static class PluginHostBuilderEx
    {
        public static THost ConfigureServices<THost>(this THost host, 
            Func<IServiceCollection, IServiceCollection> configurator)
            where THost : IPluginHostBuilder
        {
            host.Services = configurator.Invoke(host.Services);
            return host;
        }
        
        public static THost UseDefaultServices<THost>(this THost host)
            where THost : IPluginHostBuilder
        {
            host.Implementation.UseDefaultServices();
            return host;
        }
        
        public static THost UsePluginConfiguration<THost>(this THost host, 
            IPluginConfiguration pluginConfiguration)
            where THost : IPluginHostBuilder
        {
            host.PluginConfiguration = pluginConfiguration;
            return host;
        }
        
        public static THost AddPluginTypes<THost>(this THost host, 
            params Type[] pluginTypes)
            where THost : IPluginHostBuilder
        {
            foreach (var pluginType in pluginTypes)
                host.PluginTypes.Add(pluginType);
            return host;
        }
    }
}
