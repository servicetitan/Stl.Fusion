using Stl.Plugins.Metadata;

namespace Stl.Plugins;

public interface IPluginHost : IServiceProvider, IAsyncDisposable, IDisposable
{
    PluginSetInfo FoundPlugins { get; }
    // Return actual IServiceProvider hosting plugins
    IServiceProvider Services { get; }
}

public class PluginHost(IServiceProvider services) : IPluginHost
{
    public IServiceProvider Services { get; } = services;
    public PluginSetInfo FoundPlugins { get; } = services.GetRequiredService<PluginSetInfo>();

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
