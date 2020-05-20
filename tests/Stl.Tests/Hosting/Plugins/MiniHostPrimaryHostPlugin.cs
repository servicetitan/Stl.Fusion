using System;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Stl.Hosting;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Tests.Hosting.Plugins;

[assembly: Plugin(typeof(MiniHostPrimaryHostPlugin))]

namespace Stl.Tests.Hosting.Plugins
{
    public class MiniHostPrimaryHostPlugin : PrimaryHostPlugin, IMiniHostCorePlugin
    {
        protected MiniHostBuilder MiniHostBuilder { get; } = null!;

        public MiniHostPrimaryHostPlugin() { }
        public MiniHostPrimaryHostPlugin(IPluginHost plugins, IAppHostBuilder appHostBuilder)
            : base(plugins, appHostBuilder)
        {
            MiniHostBuilder = (MiniHostBuilder) appHostBuilder;
        }

        protected override void ConfigureServices()
        {
            base.ConfigureServices();
            // Regular service registrations
            HostBuilder.ConfigureServices((ctx, services) => {
                // Regular service registrations
                services.AddMediatR(GetType().Assembly);
            }).ConfigureContainer<ContainerBuilder>((ctx, containerBuilder) => {
                // Autofac service registrations
            });
        }

        protected override void ConfigureWebApp(WebHostBuilderContext ctx, IApplicationBuilder app)
        {
            base.ConfigureWebApp(ctx, app);
            app.UseWebSockets(new WebSocketOptions() {
                KeepAliveInterval = TimeSpan.FromSeconds(60),
                ReceiveBufferSize = 16_384,
            });
        }
    }
}
