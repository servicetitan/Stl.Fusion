using System.Diagnostics.CodeAnalysis;
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

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public PredefinedPluginFinder(
        Options settings,
        IPluginInfoProvider pluginInfoProvider)
        // ReSharper disable once ConvertToPrimaryConstructor
    {
        FoundPlugins = new(
            settings.PluginTypes.Distinct(),
            pluginInfoProvider,
            settings.ResolveIndirectDependencies);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    public Task Run(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
