using System.CommandLine.Builder;
using Stl.Extensibility;

namespace Stl.Bootstrap.Cli 
{
    public interface ICliPlugin
    {
        void Use(CliPluginInvocation invocation);
    }

    public class CliPluginInvocation : ChainInvocationBase<ICliPlugin, CliPluginInvocation>
    {
        public CommandLineBuilder Builder { get; set; } = default!;
    }
}
