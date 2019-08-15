using System.Collections.Immutable;
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

        public override string ToString() 
            => $"{GetType().Name} of [{Exports.ToDelimitedString()}], {Plugins.Count} plugin(s)";
    }
}
