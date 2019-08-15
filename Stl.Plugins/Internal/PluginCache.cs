using System;
using System.Collections.Concurrent;

namespace Stl.Plugins.Internal
{
    public interface IPluginCache
    {
        PluginHandle GetOrCreate(Type pluginImplementationType);
    }

    public class PluginCache : IPluginCache
    {
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<Type, PluginHandle> _cache =
            new ConcurrentDictionary<Type, PluginHandle>();

        public PluginCache(IServiceProvider services) => _services = services;

        public PluginHandle GetOrCreate(Type pluginImplementationType)
            => _cache.GetOrAdd(
                pluginImplementationType, 
                (pit, self) => {
                    var handleType = typeof(PluginHandle<>).MakeGenericType(pit);
                    return (PluginHandle) self._services.GetService(handleType);
                }, this);
    }
}
