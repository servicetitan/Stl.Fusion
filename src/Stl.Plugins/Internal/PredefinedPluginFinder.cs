using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal;

public class PredefinedPluginFinder : IPluginFinder
{
    public record Options
    {
        public IEnumerable<Type> PluginTypes { get; init; } = Enumerable.Empty<Type>();
    }

    public PluginSetInfo FoundPlugins { get; }

    public PredefinedPluginFinder(Options options, IPluginInfoProvider pluginInfoProvider)
    {
        var pluginTypes = new HashSet<Type>(options.PluginTypes);
        FoundPlugins = new PluginSetInfo(pluginTypes, pluginInfoProvider);
    }

    public Task Run(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
