using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Hosting;
using Stl.Plugins;
using Stl.Plugins.Extensions.Hosting;
using Stl.Tests.Hosting.Plugins;

[assembly: Plugin(typeof(NoopMiniHostPlugin))]

namespace Stl.Tests.Hosting.Plugins
{
    public class NoopMiniHostPlugin : IMiniHostPlugin
    {
        protected ILogger Log = NullLogger.Instance;
        protected IPluginHost Plugins { get; set; } = null!;
        protected MiniHostBuilder MiniHostBuilder { get; set; } = null!;

        public NoopMiniHostPlugin() { }
        public NoopMiniHostPlugin(
            IPluginHost plugins, 
            IAppHostBuilder appHostBuilder,
            ILogger<NoopMiniHostPlugin>? log = null)
        {
            Log = ((ILogger?) log) ?? NullLogger.Instance; 
            Plugins = plugins;
            MiniHostBuilder = (MiniHostBuilder) appHostBuilder;
        }

        public virtual void Use(HostPluginInvoker invoker)
        { }
    }
}
