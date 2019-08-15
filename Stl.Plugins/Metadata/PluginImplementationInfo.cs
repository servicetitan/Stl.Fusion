using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Stl.Reflection;

namespace Stl.Plugins.Metadata
{
    public class PluginImplementationInfo
    {
        public TypeRef Type { get; }
        public ImmutableArray<TypeRef> Ancestors { get; }
        public ImmutableArray<TypeRef> Interfaces { get; }
        public ImmutableHashSet<TypeRef> CastableTo { get; }

        [JsonConstructor]
        public PluginImplementationInfo(TypeRef type, 
            ImmutableArray<TypeRef> ancestors, 
            ImmutableArray<TypeRef> interfaces, 
            ImmutableHashSet<TypeRef> castableTo)
        {
            Type = type;
            Ancestors = ancestors;
            Interfaces = interfaces;
            CastableTo = castableTo;
        }

        public PluginImplementationInfo(Type type)
        {
            Type = type;
            Ancestors = ImmutableArray.Create(
                type.GetAllBaseTypes().Select(t => (TypeRef) t).ToArray());
            Interfaces = ImmutableArray.Create(
                type.GetInterfaces().Select(t => (TypeRef) t).ToArray());
            CastableTo = ImmutableHashSet.Create(
                Ancestors.AddRange(Interfaces).Add(type).ToArray());
        }

        public override string ToString() => $"{GetType().Name}({Type})";
    }
}
