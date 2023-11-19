using System.Diagnostics.CodeAnalysis;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins;

public interface IPluginFinder
{
    PluginSetInfo? FoundPlugins { get; }

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
    Task Run(CancellationToken cancellationToken = default);
}
