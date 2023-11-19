using System.Diagnostics.CodeAnalysis;
using Stl.Plugins.Metadata;
using Stl.Plugins.Internal;

namespace Stl.Plugins;

public static class PluginHostExt
{
    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    internal static object GetPluginInstance(
        this IPluginHost plugins, Type implementationType)
        => plugins
            .GetRequiredService<IPluginCache>()
            .GetOrCreate(implementationType)
            .Instance;

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static TPlugin GetSingletonPlugin<TPlugin>(this IPluginHost plugins)
        where TPlugin : ISingletonPlugin
        => plugins.GetPlugins<TPlugin>().Single();

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static object GetSingletonPlugin(this IPluginHost plugins, Type pluginType)
        => plugins.GetPlugins(pluginType).Single();

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static IEnumerable<TPlugin> GetPlugins<TPlugin>(this IPluginHost plugins)
        => plugins
            .GetRequiredService<IPluginHandle<TPlugin>>()
            .Instances;

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static IEnumerable<TPlugin> GetPlugins<TPlugin>(
        this IPluginHost plugins, Func<PluginInfo, bool> predicate)
        => plugins
            .GetRequiredService<IPluginHandle<TPlugin>>()
            .GetInstances(predicate);

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static IEnumerable<object> GetPlugins(
        this IPluginHost plugins, Type pluginType)
    {
        var pluginHandle = (IPluginHandle)plugins.GetRequiredService(
            typeof(IPluginHandle<>).MakeGenericType(pluginType));
        return pluginHandle.Instances;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public static IEnumerable<object> GetPlugins(
        this IServiceProvider plugins, Type pluginType, Func<PluginInfo, bool> predicate)
    {
        var pluginHandle = (IPluginHandle) plugins.GetRequiredService(
            typeof(IPluginHandle<>).MakeGenericType(pluginType));
        return pluginHandle.GetInstances(predicate);
    }
}
