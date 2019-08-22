using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Bootstrap.Web
{
    public static class WebHostBuilderEx
    {
        public static IWebHostBuilder UsePlugins(
            this IWebHostBuilder builder,
            IEnumerable<IWebHostBuilderPlugin> plugins) 
            => new WebHostBuilderPluginConfigureInvocation() {
                Tail = plugins.ToArray(),
                Handler = (plugin, invocation1) => plugin.Configure(invocation1),
                Builder = builder,
            }.Invoke().Builder;
    }
}
