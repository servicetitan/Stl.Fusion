using System;
using System.Collections.Concurrent;

namespace Stl.Plugins.Internal
{
    public interface IPluginCache
    {
        IPluginHandle GetOrCreate(Type pluginImplementationType);
    }

    public class PluginCache : IPluginCache
    {
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<Type, IPluginHandle> _cache =
            new ConcurrentDictionary<Type, IPluginHandle>();

        public PluginCache(IServiceProvider services) => _services = services;

        public IPluginHandle GetOrCreate(Type pluginImplementationType)
            => _cache.GetOrAdd(
                pluginImplementationType, 
                (pit, self) => {
                    var handleType = typeof(IPluginHandle<>).MakeGenericType(pit);
                    return (IPluginHandle) self._services.GetService(handleType);
                }, this);
    }
}
