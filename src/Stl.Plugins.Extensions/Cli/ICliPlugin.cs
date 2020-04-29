using System.CommandLine.Builder;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Cli 
{
    public interface ICliPlugin
    {
        void Use(CliPluginInvoker invoker);
    }

    public class CliPluginInvoker : InvokerBase<ICliPlugin, CliPluginInvoker>
    {
        public CommandLineBuilder Builder { get; set; } = default!;
    }
}
