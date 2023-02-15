namespace Stl.Plugins.Internal;

public interface IPluginCache
{
    IPluginInstanceHandle GetOrCreate(Type pluginImplementationType);
}

public class PluginCache : IPluginCache
{
    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<Type, IPluginInstanceHandle> _cache = new();

    public PluginCache(IServiceProvider services) => _services = services;

    public IPluginInstanceHandle GetOrCreate(Type pluginImplementationType)
        => _cache.GetOrAdd(
            pluginImplementationType,
            (pit, self) => {
                var handleType = typeof(IPluginInstanceHandle<>).MakeGenericType(pit);
                return (IPluginInstanceHandle) self._services.GetRequiredService(handleType);
            }, this);
}
