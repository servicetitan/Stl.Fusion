using System.CommandLine.Builder;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Cli 
{
    public interface ICliPlugin
    {
        void Use(CliPluginInvocation invocation);
    }

    public class CliPluginInvocation : InvokerBase<ICliPlugin, CliPluginInvocation>
    {
        public CommandLineBuilder Builder { get; set; } = default!;
    }
}
