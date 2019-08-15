using System;
using System.Collections.Concurrent;

namespace Stl.Plugins.Internal
{
    public interface IPluginCache
    {
        IPluginImplementationHandle GetOrCreate(Type pluginImplementationType);
    }

    public class PluginCache : IPluginCache
    {
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<Type, IPluginImplementationHandle> _cache =
            new ConcurrentDictionary<Type, IPluginImplementationHandle>();

        public PluginCache(IServiceProvider services) => _services = services;

        public IPluginImplementationHandle GetOrCreate(Type pluginImplementationType)
            => _cache.GetOrAdd(
                pluginImplementationType, 
                (pit, self) => {
                    var handleType = typeof(IPluginImplementationHandle<>).MakeGenericType(pit);
                    return (IPluginImplementationHandle) self._services.GetService(handleType);
                }, this);
    }
}
