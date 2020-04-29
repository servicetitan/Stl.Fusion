using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;

namespace Stl.Plugins 
{
    public static class PluginHostBuilderEx
    {
        public static TBuilder ConfigureHostConfiguration<TBuilder>(this TBuilder builder,
            Action<IConfigurationBuilder> configurationBuilder)
            where TBuilder : IPluginHostBuilder
        {
            var cfgBuilder = builder.Implementation.CreateConfigurationBuilder();
            configurationBuilder.Invoke(cfgBuilder);
            builder.Configuration = cfgBuilder.Build();
            return builder;
        }

        public static TBuilder UseServiceProviderFactory<TBuilder>(this TBuilder builder,
            Func<TBuilder, IServiceCollection, IServiceProvider> serviceProviderFactory)
            where TBuilder : IPluginHostBuilder
        {
            builder.ServiceProviderFactory = services => serviceProviderFactory.Invoke(builder, services);
            return builder;
        }

        public static TBuilder ConfigureServices<TBuilder>(this TBuilder builder, 
            Action<TBuilder, IServiceCollection> configurator)
            where TBuilder : IPluginHostBuilder
        {
            configurator.Invoke(builder, builder.Services);
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
