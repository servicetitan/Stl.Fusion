using System.Threading;
using System.Threading.Tasks;
using Stl.Plugins.Metadata;

namespace Stl.Plugins
{
    public interface IPluginFinder
    {
        PluginSetInfo? FoundPlugins { get; }

        Task Run(CancellationToken cancellationToken = default);
    }
}
