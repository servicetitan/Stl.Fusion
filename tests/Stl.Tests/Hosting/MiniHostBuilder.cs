using System;
using Stl.Hosting;
using Stl.Plugins;
using Stl.Tests.Hosting.Plugins;

namespace Stl.Tests.Hosting
{
    public class MiniHostBuilder : AppHostBuilderBase
    {
        public override string AppName => "Stl.Tests";
        public override Type[] CorePluginTypes { get; } = {typeof(IMiniHostCorePlugin)};
        public override Type[] NonTestPluginTypes { get; } = {typeof(IMiniHostPlugin)};

        protected override void ConfigurePluginHostServices()
        {
            base.ConfigurePluginHostServices();
            BuildState.PluginHostBuilder.ConfigureServices((builder, services) => { });
        }
    }
}
