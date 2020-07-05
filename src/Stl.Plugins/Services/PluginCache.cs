using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Stl.Concurrency;

namespace Stl.Plugins.Services
{
    public interface IPluginCache
    {
        IPluginInstanceHandle GetOrCreate(Type pluginImplementationType);
    }

    public class PluginCache : IPluginCache
    {
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<Type, IPluginInstanceHandle> _cache =
            new ConcurrentDictionary<Type, IPluginInstanceHandle>();

        public PluginCache(IServiceProvider services) => _services = services;

        public IPluginInstanceHandle GetOrCreate(Type pluginImplementationType)
            => _cache.GetOrAddChecked(
                pluginImplementationType, 
                (pit, self) => {
                    var handleType = typeof(IPluginInstanceHandle<>).MakeGenericType(pit);
                    return (IPluginInstanceHandle) self._services.GetRequiredService(handleType);
                }, this);
    }
}
