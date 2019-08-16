using System;
using System.Collections.Immutable;
using System.Linq;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins 
{
    public interface IPluginContainerConfiguration
    {
        PluginSetInfo Implementations { get; }
        ImmutableArray<TypeRef> Interfaces { get; }
    }

    public class PluginContainerConfiguration : IPluginContainerConfiguration
    {
        public static PluginContainerConfiguration Empty { get; } =
            new PluginContainerConfiguration(new PluginSetInfo(Enumerable.Empty<Type>()));

        public PluginSetInfo Implementations { get; }
        public ImmutableArray<TypeRef> Interfaces { get; }

        public PluginContainerConfiguration(PluginSetInfo implementations, ImmutableArray<TypeRef> interfaces)
        {
            Implementations = implementations;
            Interfaces = interfaces;
        }

        public PluginContainerConfiguration(PluginSetInfo implementations, params TypeRef[] interfaces)
        {
            Implementations = implementations;
            Interfaces = ImmutableArray.Create(interfaces);
        }

        public override string ToString() => $"{GetType().Name} " +
            $"of [{Interfaces.ToDelimitedString()}] plugin interfaces(s) " +
            $"and {Implementations.Plugins} implementation(s)";
    }
}
