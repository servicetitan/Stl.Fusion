using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;

namespace Stl.Plugins
{
    public static class PluginHostEx
    {
        internal static object GetPluginInstance(
            this IPluginHost plugins, Type implementationType) 
            => plugins
                .GetRequiredService<IPluginCache>()
                .GetOrCreate(implementationType)
                .Instance;

        public static TPlugin GetSingletonPlugin<TPlugin>(this IPluginHost plugins)
            where TPlugin : ISingletonPlugin
            => plugins.GetPlugins<TPlugin>().Single();

        public static object GetSingletonPlugin(this IPluginHost plugins, Type pluginType)
            => plugins.GetPlugins(pluginType).Single();

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(this IPluginHost plugins) 
            => plugins
                .GetRequiredService<IPluginHandle<TPlugin>>()
                .Instances;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
            this IPluginHost plugins, Func<PluginInfo, bool> predicate) 
            => plugins
                .GetRequiredService<IPluginHandle<TPlugin>>()
                .GetInstances(predicate);

        public static IEnumerable<object> GetPlugins(
            this IPluginHost plugins, Type pluginType)
        {
            var pluginHandle = (IPluginHandle) plugins.GetRequiredService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.Instances;
        }

        public static IEnumerable<object> GetPlugins(
            this IServiceProvider plugins, Type pluginType, Func<PluginInfo, bool> predicate)
        {
            var pluginHandle = (IPluginHandle) plugins.GetRequiredService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.GetInstances(predicate);
        }
    }
}
