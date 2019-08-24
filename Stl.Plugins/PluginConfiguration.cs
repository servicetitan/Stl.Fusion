using System;
using System.Collections.Immutable;
using System.Linq;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins 
{
    public interface IPluginConfiguration
    {
        PluginSetInfo Implementations { get; }
        ImmutableArray<TypeRef> Interfaces { get; }
    }

    public class PluginConfiguration : IPluginConfiguration
    {
        public static PluginConfiguration Empty { get; } =
            new PluginConfiguration(new PluginSetInfo(Enumerable.Empty<Type>()));

        public PluginSetInfo Implementations { get; }
        public ImmutableArray<TypeRef> Interfaces { get; }

        public PluginConfiguration(PluginSetInfo implementations, ImmutableArray<TypeRef> interfaces)
        {
            Implementations = implementations;
            Interfaces = interfaces;
        }

        public PluginConfiguration(PluginSetInfo implementations)
            : this(implementations, implementations.TypesByBaseType.Keys.ToImmutableArray())
        { }

        public override string ToString() => $"{GetType().Name} " +
            $"of {Interfaces.Length} plugin interfaces(s) " +
            $"and {Implementations.Plugins.Count} implementation(s)";
    }
}
