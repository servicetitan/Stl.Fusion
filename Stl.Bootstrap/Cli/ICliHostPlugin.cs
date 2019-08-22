using System.CommandLine;
using Stl.Extensibility;

namespace Stl.Bootstrap.Cli 
{
    public interface ICliHostPlugin
    {
        void Configure(CliHostPluginConfigureInvocation invocation);
    }

    public class CliHostPluginConfigureInvocation
        : ChainInvocationBase<ICliHostPlugin, CliHostPluginConfigureInvocation>
    {
        public RootCommand RootCommand { get; set; } = new RootCommand();
    }
}
