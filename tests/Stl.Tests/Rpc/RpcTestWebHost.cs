using System.Reflection;
using Microsoft.Extensions.Hosting;
using Stl.Rpc;
using Stl.Rpc.Server;

#if NETCOREAPP
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#else
using Owin;
#endif

namespace Stl.Tests.Rpc;

public class RpcTestWebHost : TestWebHostBase
{
    public IServiceCollection BaseServices { get; }
    public Assembly? ControllerAssembly { get; set; }

    public RpcTestWebHost(IServiceCollection baseServices, Assembly? controllerAssembly = null)
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
            services.AddRpc().AddWebSocketServer();
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
                endpoints.MapRpcServer();
            });
        });
    }
#else
    protected override void ConfigureAppBuilder(IServiceProvider services, IAppBuilder builder)
    {
        base.ConfigureAppBuilder(services, builder);
        builder.MapRpcServer(services);
    }
#endif
}
