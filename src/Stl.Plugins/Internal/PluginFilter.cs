using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal;

public interface IPluginFilter
{
    bool IsEnabled(PluginInfo pluginInfo);
}

public class PredicatePluginFilter : IPluginFilter
{
    private readonly Func<PluginInfo, bool> _predicate;

    public bool IsEnabled(PluginInfo pluginInfo) => _predicate(pluginInfo);

    public PredicatePluginFilter(Func<PluginInfo, bool> predicate) => _predicate = predicate;
}
