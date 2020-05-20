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
        private readonly ILogger<NoopMiniHostPlugin> _log = NullLogger<NoopMiniHostPlugin>.Instance;
        protected IPluginHost Plugins { get; set; } = null!;
        protected MiniHostBuilder MiniHostBuilder { get; set; } = null!;

        public NoopMiniHostPlugin() { }
        public NoopMiniHostPlugin(
            IPluginHost plugins, 
            IAppHostBuilder appHostBuilder,
            ILogger<NoopMiniHostPlugin>? log = null)
        {
            _log = log ??= NullLogger<NoopMiniHostPlugin>.Instance; 
            Plugins = plugins;
            MiniHostBuilder = (MiniHostBuilder) appHostBuilder;
        }

        public virtual void Use(HostPluginInvoker invoker)
        { }
    }
}
