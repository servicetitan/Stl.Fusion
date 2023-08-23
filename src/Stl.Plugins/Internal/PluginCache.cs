namespace Stl.Plugins.Internal;

public interface IPluginCache
{
    IPluginInstanceHandle GetOrCreate(Type pluginImplementationType);
}

public class PluginCache(IServiceProvider services) : IPluginCache
{
    private readonly IServiceProvider _services = services;
    private readonly ConcurrentDictionary<Type, IPluginInstanceHandle> _cache = new();

    public IPluginInstanceHandle GetOrCreate(Type pluginImplementationType)
        => _cache.GetOrAdd(pluginImplementationType, static (pluginImplementationType1, self) => {
            var handleType = typeof(IPluginInstanceHandle<>).MakeGenericType(pluginImplementationType1);
            return (IPluginInstanceHandle) self._services.GetRequiredService(handleType);
        }, this);
}
