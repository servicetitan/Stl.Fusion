using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Plugins.Metadata;
using Stl.Plugins.Internal;

namespace Stl.Plugins;

public static class PluginHostBuilderExt
{
    public static TBuilder ConfigureServices<TBuilder>(this TBuilder builder,
        Action<IServiceCollection> configurator)
        where TBuilder : PluginHostBuilder
    {
        configurator(builder.Services);
        return builder;
    }

    public static TBuilder UseServiceProviderFactory<TBuilder>(this TBuilder builder,
        Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        where TBuilder : PluginHostBuilder
    {
        builder.ServiceProviderFactory = serviceProviderFactory;
        return builder;
    }

    public static TBuilder UsePlugins<TBuilder>(this TBuilder builder, params Type[] pluginTypes)
        where TBuilder : PluginHostBuilder
        => builder.UsePlugins((IEnumerable<Type>) pluginTypes);

    public static TBuilder UsePlugins<TBuilder>(this TBuilder builder, IEnumerable<Type> pluginTypes)
        where TBuilder : PluginHostBuilder
    {
        var services = builder.Services;
        services.RemoveAll(typeof(IPluginFinder));
        services.RemoveAll(typeof(PredefinedPluginFinder.Options));

        services.AddSingleton<IPluginFinder, PredefinedPluginFinder>();
        services.AddSingleton(_ =>
            new PredefinedPluginFinder.Options() {
                PluginTypes = pluginTypes,
            });
        return builder;
    }

    public static TBuilder UsePluginFilter<TBuilder>(this TBuilder builder, Func<PluginInfo, bool> predicate)
        where TBuilder : PluginHostBuilder
    {
        builder.Services.AddSingleton<IPluginFilter>(
            new PredicatePluginFilter(predicate));
        return builder;
    }

    public static TBuilder UsePluginFilter<TBuilder>(this TBuilder builder, params Type[] pluginTypes)
        where TBuilder : PluginHostBuilder
    {
        var pluginTypeRefs = pluginTypes.Select(t => (TypeRef) t).ToArray();
        return builder.UsePluginFilter(p => pluginTypeRefs.Any(t => p.CastableTo.Contains(t)));
    }
}
