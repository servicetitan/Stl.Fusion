using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Stl.Collections;
using Stl.Reflection;

namespace Stl.Plugins
{
    // Must be used as the only argument for plugin constructor invoked when
    // PluginInfo/PluginSetInfo queries for plugin capabilities and dependencies.
    public interface IPluginInfoProvider
    {
        ImmutableHashSet<TypeRef> GetDependencies(Type pluginType);
        ImmutableOptionSet GetCapabilities(Type pluginType);
    }

    public class PluginInfoProvider : IPluginInfoProvider
    {
        private readonly ConcurrentDictionary<Type, object?> _pluginCache = new();

        public virtual ImmutableHashSet<TypeRef> GetDependencies(Type pluginType)
        {
            var plugin = GetPlugin(pluginType);
            if (plugin is not IHasDependencies hasDependencies)
                return ImmutableHashSet<TypeRef>.Empty;
            var dependencies = hasDependencies.Dependencies;
            return dependencies.Select(t => (TypeRef) t).ToImmutableHashSet();
        }

        public virtual ImmutableOptionSet GetCapabilities(Type pluginType)
        {
            var plugin = GetPlugin(pluginType);
            if (plugin is not IHasCapabilities hasCapabilities)
                return ImmutableOptionSet.Empty;
            return hasCapabilities.Capabilities;
        }

        protected virtual object? GetPlugin(Type pluginType)
            => _pluginCache.GetOrAdd(pluginType, (type, self) => {
                var ctor = type.GetConstructor(new [] {typeof(IPluginInfoProvider)});
                return ctor?.Invoke(new object[] { self });
            }, this);
    }
}

