using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;
using Stl.Plugins;

namespace Stl.Hosting.Plugins
{
    public static class AppHostBuilderEx
    {
        public static IAppHostBuilder UsePlugins<TPlugin>(
            this IAppHostBuilder appHostBuilder,
            WebHostBuilderContext context,
            IApplicationBuilder appBuilder,
            IEnumerable<TPlugin> plugins)
            where TPlugin : class, IConfigureWebAppPlugin
            => new ConfigureWebAppPluginInvoker() {
                Tail = plugins.Cast<IConfigureWebAppPlugin>().ToArray(),
                Order = InvocationOrder.Reverse,
                Handler = (plugin, invocation1) => plugin?.Use(invocation1),
                AppHostBuilder = appHostBuilder,
                Context = context,
                AppBuilder = appBuilder,
            }.Run().AppHostBuilder;

        public static IAppHostBuilder UsePlugins<TPlugin>(
            this IAppHostBuilder appHostBuilder,
            WebHostBuilderContext context,
            IApplicationBuilder appBuilder,
            IPluginHost plugins)
            where TPlugin : class, IConfigureWebAppPlugin
            => appHostBuilder.UsePlugins(context, appBuilder, plugins.GetPlugins<TPlugin>());
    }
}
