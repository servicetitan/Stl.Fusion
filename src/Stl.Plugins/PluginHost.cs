using Stl.Plugins.Metadata;

namespace Stl.Plugins;

public interface IPluginHost : IServiceProvider, IAsyncDisposable, IDisposable
{
    PluginSetInfo FoundPlugins { get; }
    // Return actual IServiceProvider hosting plugins
    IServiceProvider Services { get; }
}

public class PluginHost : IPluginHost
{
    public PluginSetInfo FoundPlugins { get; }
    public IServiceProvider Services { get; private set; }

    public PluginHost(IServiceProvider services)
    {
        Services = services;
        FoundPlugins = services.GetRequiredService<PluginSetInfo>();
    }

    public virtual ValueTask DisposeAsync()
        => Services is IAsyncDisposable ad
            ? ad.DisposeAsync()
            : ValueTaskExt.CompletedTask;

    public virtual void Dispose()
    {
        if (Services is IDisposable d)
            d.Dispose();
    }

    public object? GetService(Type serviceType)
        => Services.GetService(serviceType);
}
