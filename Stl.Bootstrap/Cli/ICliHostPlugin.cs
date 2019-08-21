using System.CommandLine;
using Stl.Extensibility;
using Stl.Plugins;

namespace Stl.Bootstrap.Cli 
{
    public interface ICliHostPlugin
    {
        void Configure(ICallChain<CliHostPluginConfigureInvocation> chain);
    }

    public class CliHostPluginConfigureInvocation : 
        PluginInvocation<ICliHostPlugin, CliHostPluginConfigureInvocation>
    {
        public RootCommand RootCommand { get; set; } = new RootCommand();
    }
}
