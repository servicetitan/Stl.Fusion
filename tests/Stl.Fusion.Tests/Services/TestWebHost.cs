using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Fusion.Server.Authentication;
using Stl.Testing;

namespace Stl.Fusion.Tests.Services
{
    public class TestWebHost : TestWebHostBase
    {
        public IServiceProvider BaseServices { get; }

        public TestWebHost(IServiceProvider baseServices)
            => BaseServices = baseServices;

        protected override void ConfigureHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                services.AddLogging(logging => {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddDebug();
                });

                services.CopySingleton<IPublisher>(BaseServices);
                services.CopySingleton<ITimeService>(BaseServices);
                services.CopySingleton<IScreenshotService>(BaseServices);
                services.CopySingleton<IKeyValueService<string>>(BaseServices);
                services.CopySingleton<IEdgeCaseService>(BaseServices);
                // services.CopySingleton<IAuthService>(BaseServices);

                // Fusion
                var fusion = services.AddFusion();
                fusion.AddAuthentication().AddServer();
                fusion.AddWebSocketServer();

                // Web
                services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

                // Testing
                services.AddHostedService<ApplicationPartsLogger>();
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.Configure((ctx, app) => {
                app.UseWebSockets(new WebSocketOptions() { ReceiveBufferSize = 16_384 });
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
