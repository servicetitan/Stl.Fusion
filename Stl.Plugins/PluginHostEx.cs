using System;
using System.Collections.Generic;
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
                .GetService<IPluginCache>()
                .GetOrCreate(implementationType)
                .UntypedInstance;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
            this IPluginHost plugins) 
            => plugins
                .GetService<IPluginHandle<TPlugin>>()
                .Instances;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
            this IPluginHost plugins, Func<PluginInfo, bool> predicate) 
            => plugins
                .GetService<IPluginHandle<TPlugin>>()
                .GetInstances(predicate);

        public static IEnumerable<object> GetPlugins(
            this IPluginHost plugins, Type pluginType)
        {
            var pluginHandle = (IPluginHandle) plugins.GetService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.UntypedInstances;
        }

        public static IEnumerable<object> GetPlugins(
            this IServiceProvider plugins, Type pluginType, Func<PluginInfo, bool> predicate)
        {
            var pluginHandle = (IPluginHandle) plugins.GetService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.GetUntypedInstances(predicate);
        }
    }
}
