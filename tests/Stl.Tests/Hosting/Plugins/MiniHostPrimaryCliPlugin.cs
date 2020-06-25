using System.CommandLine.Invocation;
using Stl.Hosting;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Tests.Hosting.Plugins;

[assembly: Plugin(typeof(MiniHostPrimaryCliPlugin))]

namespace Stl.Tests.Hosting.Plugins
{
    public class MiniHostPrimaryCliPlugin : PrimaryCliPlugin, IMiniHostCorePlugin
    {
        protected MiniHostBuilder MiniHostBuilder { get; } = null!;

        public MiniHostPrimaryCliPlugin() { }
        public MiniHostPrimaryCliPlugin(IPluginHost plugins, IAppHostBuilder appHostBuilder)
            : base(plugins, appHostBuilder)
        {
            MiniHostBuilder = (MiniHostBuilder) appHostBuilder;
        }

        protected override void ConfigureCliBuilder()
        {
            base.ConfigureCliBuilder();
            var rootCommand = CliBuilder.Command;
            rootCommand.Handler = CommandHandler.Create(() => {
                AppHostBuilder.BuildState.BuildHost();
            });
        }
    }
}
