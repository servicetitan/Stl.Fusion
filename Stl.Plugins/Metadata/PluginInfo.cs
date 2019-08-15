using System.Collections.Immutable;
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

        public override string ToString() => $"{GetType().Name}({Type})";
    }
}
