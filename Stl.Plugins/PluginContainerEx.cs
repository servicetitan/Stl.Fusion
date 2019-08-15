using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins
{
    public static class PluginContainerEx
    {
        internal static object GetPluginImplementation(this IServiceProvider services, Type pluginType) 
            => services.GetService<PluginCache>().GetOrCreate(pluginType);

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, TypeRef type)
            => services.GetPlugins(type.Resolve());

        public static IEnumerable<object> GetPlugins(this IServiceProvider services, Type type)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            if (!pluginSetInfo.Exports.Contains(type))
                throw Errors.UnknownPluginType(type.Name);
            foreach (var typeRef in pluginSetInfo.Implementations[type])
                yield return services.GetPluginImplementation(typeRef.Resolve());
        }

        public static IEnumerable<TPlugin> GetPlugins<TPlugin>(this IServiceProvider services) 
            => services.GetPlugins(typeof(TPlugin)).Cast<TPlugin>();
    }
}
