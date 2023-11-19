using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Plugins.Internal;

namespace Stl.Plugins;

public class PluginHostBuilder
{
    public IServiceCollection Services { get; set; }
    public Func<IServiceCollection, IServiceProvider> ServiceProviderFactory { get; set; } =
        services => new DefaultServiceProviderFactory().CreateServiceProvider(services);

    public PluginHostBuilder(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        Services = services;
        if (!services.HasService<ILoggerFactory>())
            services.AddLogging();

        // Own services
        services.TryAddSingleton<IPluginHost>(c => new PluginHost(c));
        services.TryAddSingleton<IPluginFactory>(c => new PluginFactory(c));
        services.TryAddSingleton<IPluginCache>(c => new PluginCache(c));
        services.TryAddSingleton<IPluginInfoProvider>(_ => new PluginInfoProvider());
        services.TryAddSingleton(typeof(IPluginInstanceHandle<>), typeof(PluginInstanceHandle<>));
        services.TryAddSingleton(typeof(IPluginHandle<>), typeof(PluginHandle<>));
        services.TryAddSingleton(c => {
            var pluginFinder = c.GetRequiredService<IPluginFinder>();
            return pluginFinder.FoundPlugins
                ?? throw Errors.PluginFinderRunFailed(pluginFinder.GetType());
        });

        // FileSystemPluginFinder is the default IPluginFinder
        services.TryAddSingleton(_ => new FileSystemPluginFinder.Options());
        services.TryAddSingleton<IPluginFinder>(c => new FileSystemPluginFinder(
            c.GetRequiredService<FileSystemPluginFinder.Options>(),
            c.GetRequiredService<IPluginInfoProvider>(),
            c.LogFor<FileSystemPluginFinder>()));
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public IPluginHost Build()
        => Task.Run(() => BuildAsync()).Result;

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public virtual async Task<IPluginHost> BuildAsync(CancellationToken cancellationToken = default)
    {
        var services = ServiceProviderFactory(Services);
        var pluginFinder = services.GetRequiredService<IPluginFinder>();
        await pluginFinder.Run(cancellationToken).ConfigureAwait(false);
        var host = services.GetRequiredService<IPluginHost>();
        return host;
    }
}
