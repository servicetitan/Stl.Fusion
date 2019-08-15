using System;
using System.Collections.Immutable;
using System.Linq;
using Stl.Reflection;

namespace Stl.Plugins.Metadata
{
    public class PluginInfo
    {
        public TypeRef Type { get; }
        public ImmutableHashSet<TypeRef> BaseTypes { get; }

        public PluginInfo(TypeRef type, ImmutableHashSet<TypeRef> baseTypes)
        {
            Type = type;
            BaseTypes = baseTypes;
        }

        public PluginInfo(Type type)
        {
            Type = type;
            var baseTypes = type.GetAllBaseTypes().ToList();
            baseTypes.AddRange(type.GetInterfaces());
            var baseTypeRefs = baseTypes.Select(t => (TypeRef) t).ToArray();
            BaseTypes = ImmutableHashSet.Create(baseTypeRefs);
        }

        public override string ToString() => $"{GetType().Name}({Type})";
    }
}
