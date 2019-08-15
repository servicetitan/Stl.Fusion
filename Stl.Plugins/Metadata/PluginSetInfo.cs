using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Stl.Reflection;

namespace Stl.Plugins.Metadata 
{
    public class PluginSetInfo
    {
        public ImmutableHashSet<TypeRef> Exports { get; }
        public ImmutableDictionary<TypeRef, PluginInfo> Plugins { get; }
        public ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> Implementations { get; }

        public PluginSetInfo(ImmutableHashSet<TypeRef> exports, ImmutableDictionary<TypeRef, PluginInfo> plugins, ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> implementations)
        {
            Exports = exports;
            Plugins = plugins;
            Implementations = implementations;
        }

        public PluginSetInfo(IEnumerable<Type> exportedTypes, IEnumerable<Type> pluginTypes)
        {
            var plugins = new Dictionary<TypeRef, PluginInfo>();
            var implementations = new Dictionary<TypeRef, ImmutableHashSet<TypeRef>>();

            void AddImplementation(TypeRef baseType, TypeRef implementation)
            {
                var existing = implementations.GetValueOrDefault(baseType) 
                    ?? ImmutableHashSet<TypeRef>.Empty;
                implementations[baseType] = existing.Add(implementation);
            }

            foreach (var plugin in pluginTypes) {
                var pluginInfo = new PluginInfo(plugin);
                plugins.Add(plugin, pluginInfo);
                AddImplementation(pluginInfo.Type, pluginInfo.Type);
                foreach (var baseType in pluginInfo.BaseTypes)
                    AddImplementation(baseType, pluginInfo.Type);
            }

            Exports = ImmutableHashSet.Create(exportedTypes.Select(t => (TypeRef) t).ToArray());
            Plugins = plugins.ToImmutableDictionary();
            Implementations = implementations.ToImmutableDictionary();
        }

        public override string ToString() 
            => $"{GetType().Name} of [{Exports.ToDelimitedString()}], {Plugins.Count} plugin(s)";
    }
}
