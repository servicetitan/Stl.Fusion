using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Stl.Plugins.Internal;
using Stl.Reflection;

namespace Stl.Plugins.Metadata
{
    public class PluginInfo
    {
        public TypeRef Type { get; }
        public ImmutableArray<TypeRef> Ancestors { get; }
        public ImmutableArray<TypeRef> Interfaces { get; }
        public ImmutableHashSet<TypeRef> CastableTo { get; }
        public ImmutableDictionary<string, object> Capabilities { get; }

        [JsonConstructor]
        public PluginInfo(TypeRef type, 
            ImmutableArray<TypeRef> ancestors, 
            ImmutableArray<TypeRef> interfaces, 
            ImmutableHashSet<TypeRef> castableTo,
            ImmutableDictionary<string, object> capabilities)
        {
            Type = type;
            Ancestors = ancestors;
            Interfaces = interfaces;
            CastableTo = castableTo;
            Capabilities = capabilities;
        }

        public PluginInfo(Type type, IPluginFactory pluginFactory)
        {
            Type = type;
            Ancestors = ImmutableArray.Create(
                type.GetAllBaseTypes().Select(t => (TypeRef) t).ToArray());
            Interfaces = ImmutableArray.Create(
                type.GetInterfaces().Select(t => (TypeRef) t).ToArray());
            CastableTo = ImmutableHashSet.Create(
                Ancestors.AddRange(Interfaces).Add(type).ToArray());
            Capabilities = ImmutableDictionary<string, object>.Empty;

            if (typeof(IHasCapabilities).IsAssignableFrom(type)) {
                var tmpPlugin = pluginFactory.Create(type);
                if (tmpPlugin is IHasCapabilities hc)
                    Capabilities = hc.Capabilities;
            }
        }

        public override string ToString() => $"{GetType().Name}({Type})";
    }
}
