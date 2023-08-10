namespace Stl.Plugins.Internal;

public interface IPluginFactory
{
    object? Create(Type pluginType);
}

public class PluginFactory(IServiceProvider services) : IPluginFactory
{
    protected IServiceProvider Services { get; } = services;

    public virtual object? Create(Type pluginType)
        => Services.Activate(pluginType);
}
