using Stl.Plugins.Metadata;

namespace Stl.Plugins;

public interface IPluginFinder
{
    PluginSetInfo? FoundPlugins { get; }

    Task Run(CancellationToken cancellationToken = default);
}
