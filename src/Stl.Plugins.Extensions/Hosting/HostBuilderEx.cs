using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Hosting
{
    public static class HostBuilderEx
    {
        public static IHostBuilder UsePlugins<TPlugin>(
            this IHostBuilder builder,
            IEnumerable<TPlugin> plugins)
            where TPlugin : class, IHostPlugin
            => new HostPluginInvoker() {
                Tail = plugins.Cast<IHostPlugin>().ToArray(),
                Order = InvocationOrder.Reverse,
                Handler = (plugin, invocation1) => plugin?.Use(invocation1),
                Builder = builder,
            }.Run().Builder;

        public static IHostBuilder UsePlugins<TPlugin>(
            this IHostBuilder builder,
            IPluginHost plugins)
            where TPlugin : class, IHostPlugin
            => builder.UsePlugins(plugins.GetPlugins<TPlugin>());
    }
}
