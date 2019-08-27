using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;
using Stl.Plugins.Services;

namespace Stl.Plugins
{
    public static class PluginServiceProviderEx
    {
        internal static object GetPluginInstance(
            this IServiceProvider services, Type implementationType) 
            => services
                .GetService<IPluginCache>()
                .GetOrCreate(implementationType)
                .UntypedInstance;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
            this IServiceProvider services) 
            => services
                .GetService<IPluginHandle<TPlugin>>()
                .Instances;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
            this IServiceProvider services, Func<PluginInfo, bool> predicate) 
            => services
                .GetService<IPluginHandle<TPlugin>>()
                .GetInstances(predicate);

        public static IEnumerable<object> GetPlugins(
            this IServiceProvider services, Type pluginType)
        {
            var pluginHandle = (IPluginHandle) services.GetService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.UntypedInstances;
        }

        public static IEnumerable<object> GetPlugins(
            this IServiceProvider services, Type pluginType, Func<PluginInfo, bool> predicate)
        {
            var pluginHandle = (IPluginHandle) services.GetService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.GetUntypedInstances(predicate);
        }
    }
}
