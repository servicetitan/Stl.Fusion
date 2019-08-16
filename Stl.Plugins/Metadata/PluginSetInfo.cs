using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Plugins.Internal;
using Stl.Reflection;

namespace Stl.Plugins.Metadata 
{
    public class PluginSetInfo
    {
        public ImmutableDictionary<TypeRef, PluginInfo> Plugins { get; }
        public ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> TypesByBaseType { get; }

        [JsonConstructor]
        public PluginSetInfo(
            ImmutableDictionary<TypeRef, PluginInfo> plugins, 
            ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> typesByBaseType)
        {
            Plugins = plugins;
            TypesByBaseType = typesByBaseType;
        }

        public PluginSetInfo(IEnumerable<Type> plugins)
        {
            if (!plugins.Any()) {
                // Super important to have this case handled explicitly.
                // Otherwise the initializer for PluginContainerConfiguration.Empty
                // will fail due to recursion here & attempt to register null as a
                // singleton inside BuildContainer call below.
                Plugins = ImmutableDictionary<TypeRef, PluginInfo>.Empty;
                TypesByBaseType = ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>>.Empty;
                return;
            }

            var tmpContainer = new PluginContainerBuilder().BuildContainer();
            var tmpFactory = tmpContainer.GetService<IPluginFactory>();

            var dPlugins = new Dictionary<TypeRef, PluginInfo>();
            var dTypesByBaseType = new Dictionary<TypeRef, ImmutableHashSet<TypeRef>>();

            foreach (var plugin in plugins) {
                var pluginInfo = new PluginInfo(plugin, tmpFactory);
                dPlugins.Add(plugin, pluginInfo);
                foreach (var baseType in pluginInfo.CastableTo) {
                    var existingImpls = dTypesByBaseType.GetValueOrDefault(baseType) 
                        ?? ImmutableHashSet<TypeRef>.Empty;
                    dTypesByBaseType[baseType] = existingImpls.Add(pluginInfo.Type);
                }
            }

            Plugins = dPlugins.ToImmutableDictionary();
            TypesByBaseType = dTypesByBaseType.ToImmutableDictionary();
        }

        public override string ToString() 
            => $"{GetType().Name} of [{Plugins.Values.ToDelimitedString()}]";
    }
}
