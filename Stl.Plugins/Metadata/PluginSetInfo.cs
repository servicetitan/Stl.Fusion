using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Stl.Reflection;

namespace Stl.Plugins.Metadata 
{
    public class PluginSetInfo
    {
        public ImmutableHashSet<TypeRef> Exports { get; }
        public ImmutableDictionary<TypeRef, PluginImplementationInfo> Implementations { get; }
        public ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> ImplementationsByBaseType { get; }

        [JsonConstructor]
        public PluginSetInfo(ImmutableHashSet<TypeRef> exports, 
            ImmutableDictionary<TypeRef, PluginImplementationInfo> implementations, 
            ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> implementationsByBaseType)
        {
            Exports = exports;
            Implementations = implementations;
            ImplementationsByBaseType = implementationsByBaseType;
        }

        public PluginSetInfo(IEnumerable<Type> exports, IEnumerable<Type> implementations)
        {
            var impls = new Dictionary<TypeRef, PluginImplementationInfo>();
            var implsByBase = new Dictionary<TypeRef, ImmutableHashSet<TypeRef>>();

            foreach (var plugin in implementations) {
                var pluginInfo = new PluginImplementationInfo(plugin);
                impls.Add(plugin, pluginInfo);
                foreach (var baseType in pluginInfo.CastableTo) {
                    var existingImpls = implsByBase.GetValueOrDefault(baseType) 
                        ?? ImmutableHashSet<TypeRef>.Empty;
                    implsByBase[baseType] = existingImpls.Add(pluginInfo.Type);
                }
            }

            Exports = ImmutableHashSet.Create(exports.Select(t => (TypeRef) t).ToArray());
            Implementations = impls.ToImmutableDictionary();
            ImplementationsByBaseType = implsByBase.ToImmutableDictionary();
        }

        public override string ToString() 
            => $"{GetType().Name} of [{Exports.ToDelimitedString()}], {Implementations.Count} plugin(s)";
    }
}
