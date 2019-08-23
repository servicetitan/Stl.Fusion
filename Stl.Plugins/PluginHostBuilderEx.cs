using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Plugins 
{
    public static class PluginHostBuilderEx
    {
        public static TBuilder ConfigureServices<TBuilder>(this TBuilder builder, 
            Func<IServiceCollection, IServiceCollection> configurator)
            where TBuilder : IPluginHostBuilder
        {
            builder.Services = configurator.Invoke(builder.Services);
            return builder;
        }
        
        public static TBuilder UseDefaultServices<TBuilder>(this TBuilder builder)
            where TBuilder : IPluginHostBuilder
        {
            builder.Implementation.UseDefaultServices();
            return builder;
        }
        
        public static TBuilder UsePluginConfiguration<TBuilder>(this TBuilder builder, 
            IPluginConfiguration pluginConfiguration)
            where TBuilder : IPluginHostBuilder
        {
            builder.PluginConfiguration = pluginConfiguration;
            return builder;
        }
        
        public static TBuilder AddPluginTypes<TBuilder>(this TBuilder builder, 
            params Type[] pluginTypes)
            where TBuilder : IPluginHostBuilder
        {
            foreach (var pluginType in pluginTypes)
                builder.PluginTypes.Add(pluginType);
            return builder;
        }
    }
}
