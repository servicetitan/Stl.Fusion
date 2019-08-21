using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;
using Stl.Plugins;

namespace Stl.Bootstrap.Web
{
    public static class WebHostBuilderEx
    {
        public static IWebHostBuilder UsePlugins(
            this IWebHostBuilder builder,
            IEnumerable<IWebHostBuilderPlugin> plugins)
        {
            var invocation =  new WebHostBuilderPluginConfigureInvocation() {
                Builder = builder,
                Plugins = plugins.ToImmutableArray(),
            };
            invocation.Invoke((plugin, chain) => plugin.Configure(chain));
            return invocation.Builder;
        }
    }
}
