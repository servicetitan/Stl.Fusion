using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Async;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Testing;

namespace Stl.Tests.Fusion.Services
{
    public class TestWebServer
    {
        public int Port { get; }
        public Uri BaseUri { get; }
        public IServiceProvider Services { get; }
        protected IPublisher Publisher => Services.GetRequiredService<IPublisher>();
        protected ITimeService TimeService => Services.GetRequiredService<ITimeService>();

        public TestWebServer(IServiceProvider services, int? port = null)
        {
            port ??= WebTestEx.GetRandomPort();
            Port = port.Value;
            BaseUri = WebTestEx.GetLocalUri(Port);
            Services = services;
        }

        public async Task<IAsyncDisposable> ServeAsync()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services => {
                    services.AddLogging(logging => {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddDebug();
                    });

                    services.AddSingleton(Publisher);
                    services.AddSingleton(TimeService);
                    services.AddFusionWebSocketServer();

                    // Web
                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
                    services.AddMvc()
                        .AddNewtonsoftJson()
                        .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

                    // Testing
                    services.AddHostedService<ApplicationPartsLogger>();
                })
                .ConfigureWebHost(builder => {
                    builder.UseKestrel().UseUrls(BaseUri.ToString());
                    builder.Configure((ctx, app) => {
                        app.UseWebSockets(new WebSocketOptions() { ReceiveBufferSize = 16_384 });

                        // API controllers
                        app.UseRouting();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapControllers();
                            endpoints.MapFusionWebSocketServer();
                        });
                    });
                })
                .Build();
            await host.StartAsync().ConfigureAwait(false);
            await Task.Delay(1000);

            // ReSharper disable once HeapView.BoxingAllocation
            return AsyncDisposable.New(async host1 => {
                await host1.StopAsync().SuppressExceptions().ConfigureAwait(false);
            }, host);
        }
    }
}
