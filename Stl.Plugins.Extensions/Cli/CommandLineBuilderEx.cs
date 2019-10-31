using System.Collections.Generic;
using System.CommandLine.Builder;
using System.Linq;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Cli
{
    public static class CommandLineBuilderEx
    {
        public static CommandLineBuilder UsePlugins<TPlugin>(
            this CommandLineBuilder builder,
            IEnumerable<TPlugin> plugins)
            where TPlugin : class, ICliPlugin
            => new CliPluginInvoker() {
                Tail = plugins.Cast<ICliPlugin>().ToArray(),
                Order = InvocationOrder.Reverse,
                Handler = (plugin, invocation1) => plugin?.Use(invocation1),
                Builder = builder,
            }.Run().Builder;

        public static CommandLineBuilder UsePlugins<TPlugin>(
            this CommandLineBuilder builder,
            IPluginHost plugins)
            where TPlugin : class, ICliPlugin
            => builder.UsePlugins(plugins.GetPlugins<TPlugin>());
    }
}
