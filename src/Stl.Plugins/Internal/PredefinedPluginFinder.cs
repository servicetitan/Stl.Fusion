using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal;

public class PredefinedPluginFinder : IPluginFinder
{
    public record Options
    {
        public IEnumerable<Type> PluginTypes { get; init; } = Enumerable.Empty<Type>();
        public bool ResolveIndirectDependencies { get; init; }
    }

    public PluginSetInfo FoundPlugins { get; }

    public PredefinedPluginFinder(Options options, IPluginInfoProvider pluginInfoProvider) 
        => FoundPlugins = new PluginSetInfo(
            options.PluginTypes.Distinct(),
            pluginInfoProvider,
            options.ResolveIndirectDependencies);

    public Task Run(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
