using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Reflection;
using Stl.Serialization;
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
                services.AddFusionWebSocketServer();

                // Web
                services.AddRouting();
                services.AddControllers()
                    .AddApplicationPart(Assembly.GetExecutingAssembly());
                services.AddMvc()
                    .AddNewtonsoftJson(options => {
                        MemberwiseCopier
                            .New(JsonNetSerializer.DefaultSettings)
                            .Apply(options.SerializerSettings);
                    });

                // Testing
                services.AddHostedService<ApplicationPartsLogger>();
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.Configure((ctx, app) => {
                app.UseWebSockets(new WebSocketOptions() { ReceiveBufferSize = 16_384 });

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
