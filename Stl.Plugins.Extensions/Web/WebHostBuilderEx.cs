using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Web
{
    public static class WebHostBuilderEx
    {
        public static IWebHostBuilder UsePlugins<TPlugin>(
            this IWebHostBuilder builder,
            IEnumerable<TPlugin> plugins)
            where TPlugin : IWebHostPlugin
            => new WebHostPluginInvocation() {
                Tail = plugins.Cast<IWebHostPlugin>().ToArray(),
                Order = InvocationOrder.Reverse,
                Handler = (plugin, invocation1) => plugin.Use(invocation1),
                Builder = builder,
            }.Run().Builder;

        public static IWebHostBuilder UsePlugins<TPlugin>(
            this IWebHostBuilder builder,
            IServiceProvider pluginHost)
            where TPlugin : IWebHostPlugin
            => builder.UsePlugins(pluginHost.GetPlugins<TPlugin>());
    }
}
