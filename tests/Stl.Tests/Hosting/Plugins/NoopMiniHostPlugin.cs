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
        private readonly ILogger _log;
        protected IPluginHost Plugins { get; set; }
        protected MiniHostBuilder MiniHostBuilder { get; set; }

        public NoopMiniHostPlugin() : this(null!, null!) { }
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
