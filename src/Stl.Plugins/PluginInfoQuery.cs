using Stl.Plugins.Metadata;

namespace Stl.Plugins
{
    // Must be used as the only argument for plugin constructor invoked when
    // PluginInfo/PluginSetInfo queries for plugin capabilities and dependencies.
    public interface IPluginInfoQuery
    { }

    public class PluginInfoQuery : IPluginInfoQuery
    { }
}

