using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;

namespace Stl.Plugins 
{
    public static class PluginHostBuilderEx
    {
        public static TBuilder UseServiceProviderFactory<TBuilder>(this TBuilder builder,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
            where TBuilder : IPluginHostBuilder
        {
            builder.ServiceProviderFactory = serviceProviderFactory;
            return builder;
        }
        
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
        
        public static TBuilder UsePlugins<TBuilder>(this TBuilder builder, 
            PluginSetInfo plugins)
            where TBuilder : IPluginHostBuilder
        {
            builder.Plugins = plugins;
            return builder;
        }
        
        public static TBuilder UsePluginTypes<TBuilder>(this TBuilder builder, 
            params Type[] pluginTypes)
            where TBuilder : IPluginHostBuilder
        {
            builder.PluginTypes = new HashSet<Type>(pluginTypes);
            return builder;
        }

        public static TBuilder AddPluginFilter<TBuilder>(this TBuilder builder, 
            Func<PluginInfo, bool> predicate)
            where TBuilder : IPluginHostBuilder
        {
            builder.Services.AddSingleton<IPluginFilter>(new PredicatePluginFilter(predicate));
            return builder;
        }

        public static TBuilder SetAutoStart<TBuilder>(this TBuilder builder, bool runAutoStart)
            where TBuilder : IPluginHostBuilder
        {
            builder.AutoStart = runAutoStart;
            return builder;
        }
    }
}
