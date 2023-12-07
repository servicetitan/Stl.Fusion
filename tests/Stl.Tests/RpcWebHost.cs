using System.Reflection;
using Microsoft.Extensions.Hosting;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Server;
using Stl.Rpc.WebSockets;

#if NETFRAMEWORK
using Owin;
using System.Web.Http;
using Stl.Fusion.Server;
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#endif

namespace Stl.Tests;

public class RpcWebHost : TestWebHostBase
{
    public IServiceCollection BaseServices { get; }
    public Assembly? ControllerAssembly { get; set; }
    public TimeSpan WebSocketWriteDelay { get; set; }

    public RpcWebHost(IServiceCollection baseServices, Assembly? controllerAssembly = null)
    {
        BaseServices = baseServices;
        ControllerAssembly = controllerAssembly;
    }

    protected override void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            // Copy all services from the base service provider here
            services.AddRange(BaseServices);

            // Since we copy all services here,
            // only web-related ones must be added to services
            var webSocketServer = services.AddRpc().AddWebSocketServer();
            webSocketServer.Configure(_ => {
                var defaultOptions = RpcWebSocketServer.Options.Default;
                return defaultOptions with {
                    WebSocketChannelOptions = defaultOptions.WebSocketChannelOptions with {
                        WriteDelay = WebSocketWriteDelay,
                    },
                };
            });
            if (ControllerAssembly != null) {
#if NETFRAMEWORK
                var controllerTypes = ControllerAssembly.GetControllerTypes().ToArray();
                services.AddControllersAsServices(controllerTypes);
#else
                services.AddControllers().AddApplicationPart(ControllerAssembly);
                services.AddHostedService<ApplicationPartsLogger>();
#endif
            }
        });
    }

#if NETCOREAPP
    protected override void ConfigureWebHost(IWebHostBuilder webHost)
    {
        webHost.Configure((_, app) => {
            app.UseWebSockets();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "DefaultApi", pattern: "api/{controller}/{action}");
                endpoints.MapControllers();
                endpoints.MapRpcWebSocketServer();
            });
        });
    }
#else
    protected override void ConfigureHttp(IServiceProvider services, HttpConfiguration config)
    {
        base.ConfigureHttp(services, config);
        config.Formatters.Insert(0, new TextMediaTypeFormatter());
    }

    protected override void ConfigureAppBuilder(IServiceProvider services, IAppBuilder builder)
    {
        base.ConfigureAppBuilder(services, builder);
        builder.MapRpcServer(services);
    }
#endif
}
