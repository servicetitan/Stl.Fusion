using System;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Services
{
    public interface IPluginFilter
    {
        bool IsEnabled(PluginInfo pluginInfo);
    }

    public class PredicatePluginFilter : IPluginFilter
    {
        private readonly Func<PluginInfo, bool> _predicate;

        public bool IsEnabled(PluginInfo pluginInfo) => _predicate.Invoke(pluginInfo);

        public PredicatePluginFilter(Func<PluginInfo, bool> predicate) => _predicate = predicate;
    }
}
