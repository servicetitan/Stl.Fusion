using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Stl.Collections;
using Stl.Fusion.Server;
using Stl.Fusion.Server.Authentication;
using Stl.Testing;

namespace Stl.Fusion.Tests.Services
{
    public class FusionTestWebHost : TestWebHostBase
    {
        public IServiceCollection BaseServices { get; }

        public FusionTestWebHost(IServiceCollection baseServices)
            => BaseServices = baseServices;

        protected override void ConfigureHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                // Copy all services from the base service provider here
                services.AddRange(BaseServices);

                // Since we copy all services here,
                // only web-related ones must be added to services
                services.AddFusion(fusion => {
                    fusion.AddWebSocketServer();
                    fusion.AddAuthentication(auth => auth.AddServer());
                });

                // Web
                services.AddRouting();
                services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
                services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

                // Testing
                services.AddHostedService<ApplicationPartsLogger>();
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.Configure((ctx, app) => {
                app.UseWebSockets();
                app.UseFusionSession();

                // API controllers
                app.UseRouting();
                app.UseEndpoints(endpoints => {
                    endpoints.MapControllers();
                    endpoints.MapFusionWebSocketServer();
                });
            });
        }
    }
}
