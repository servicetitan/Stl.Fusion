using Microsoft.Extensions.Hosting;
using Stl.Rpc;
using Stl.Rpc.Server;

#if NETCOREAPP
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#else
using Owin;
#endif

namespace Stl.Tests.Rpc;

public class RpcTestWebHost : TestWebHostBase
{
    public IServiceCollection BaseServices { get; }

    public RpcTestWebHost(IServiceCollection baseServices)
        => BaseServices = baseServices;

    protected override void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            // Copy all services from the base service provider here
            services.AddRange(BaseServices);

            // Since we copy all services here,
            // only web-related ones must be added to services
            services.AddRpc().AddServer();
        });
    }

#if NETCOREAPP
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.Configure((_, app) => {
            app.UseWebSockets();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapRpcServer());
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
