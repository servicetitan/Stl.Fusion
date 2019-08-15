using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;
using Stl.Reflection;

namespace Stl.Plugins
{
    public static class PluginContainerEx
    {
        internal static object GetPluginInstance(this IServiceProvider services, Type implementationType) 
            => services
                .GetService<IPluginCache>()
                .GetOrCreate(implementationType)
                .UntypedInstance;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(this IServiceProvider services) 
            => services
                .GetService<IPluginHandle<TPlugin>>()
                .Instances;

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, TypeRef pluginType)
            => services.GetPlugins(pluginType.Resolve());

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, Type pluginType)
        {
            var pluginHandle = (IPluginHandle) services.GetService(
                typeof(IPluginHandle<>).MakeGenericType(pluginType));
            return pluginHandle.UntypedInstances;
        }
    }
}
