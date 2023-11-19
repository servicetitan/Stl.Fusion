using System.Diagnostics.CodeAnalysis;
using Stl.Plugins.Internal;

namespace Stl.Plugins;

// Must be used as the only argument for plugin constructor invoked when
// PluginInfo/PluginSetInfo queries for plugin capabilities and dependencies.
public interface IPluginInfoProvider
{
    /// <summary>
    /// Use this type as a plugin constructor parameter in
    /// "info query" constructors.
    /// </summary>
#pragma warning disable CA1052
    public class Query
#pragma warning restore CA1052
    {
        public static readonly Query Instance = new();
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    ImmutableHashSet<TypeRef> GetDependencies(Type pluginType);
    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    ImmutableOptionSet GetCapabilities(Type pluginType);
}

public class PluginInfoProvider : IPluginInfoProvider
{
    private readonly ConcurrentDictionary<Type, object?> _pluginCache = new();

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public virtual ImmutableHashSet<TypeRef> GetDependencies(Type pluginType)
    {
        var plugin = GetPlugin(pluginType);
        if (plugin is not IHasDependencies hasDependencies)
            return ImmutableHashSet<TypeRef>.Empty;
        var dependencies = hasDependencies.Dependencies;
        return dependencies.Select(t => (TypeRef) t).ToImmutableHashSet();
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public virtual ImmutableOptionSet GetCapabilities(Type pluginType)
    {
        var plugin = GetPlugin(pluginType);
        if (plugin is not IHasCapabilities hasCapabilities)
            return ImmutableOptionSet.Empty;
        return hasCapabilities.Capabilities;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    protected virtual object? GetPlugin(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pluginType)
        => _pluginCache.GetOrAdd(pluginType, static (type, self) => {
#pragma warning disable IL2070
            var ctor = type.GetConstructor(new [] {typeof(IPluginInfoProvider.Query)});
            if (ctor != null)
                return ctor.Invoke(new object[] { IPluginInfoProvider.Query.Instance });
            ctor = type.GetConstructor(Type.EmptyTypes);
            return ctor?.Invoke(Array.Empty<object>());
#pragma warning restore IL2070
        }, this);
}
