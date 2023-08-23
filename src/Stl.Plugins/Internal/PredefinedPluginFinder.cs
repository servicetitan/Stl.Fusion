using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal;

public class PredefinedPluginFinder(
        PredefinedPluginFinder.Options settings,
        IPluginInfoProvider pluginInfoProvider
        ) : IPluginFinder
{
    public record Options
    {
        public IEnumerable<Type> PluginTypes { get; init; } = Enumerable.Empty<Type>();
        public bool ResolveIndirectDependencies { get; init; }
    }

    public PluginSetInfo FoundPlugins { get; } = new(
        settings.PluginTypes.Distinct(),
        pluginInfoProvider,
        settings.ResolveIndirectDependencies);

    public Task Run(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
