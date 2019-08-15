using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;
using Stl.Reflection;

namespace Stl.Plugins
{
    public static class PluginContainerEx
    {
        internal static object GetPluginImplementation(this IServiceProvider services, Type pluginType) 
            => services
                .GetService<IPluginCache>()
                .GetOrCreate(pluginType)
                .UntypedInstance;

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(this IServiceProvider services) 
            => services
                .GetService<IPluginHandle<TPlugin>>()
                .Implementations;

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, TypeRef type)
            => services.GetPlugins(type.Resolve());

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, Type type)
        {
            var pluginHandle = (IPluginHandle) services.GetService(
                typeof(IPluginHandle<>).MakeGenericType(type));
            return pluginHandle.UntypedImplementations;
        }
    }
}
