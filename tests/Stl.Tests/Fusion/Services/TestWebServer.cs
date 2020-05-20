using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Red;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Testing;

namespace Stl.Tests.Fusion.Services
{
    public class TestWebServer
    {
        public int Port { get; }
        public Uri BaseUri { get; }
        protected IPublisher Publisher { get; }

        public TestWebServer(IPublisher publisher, int? port = null)
        {
            port ??= WebTestEx.GetRandomPort(); 
            Port = port.Value; 
            BaseUri = WebTestEx.GetLocalUri(Port);
            Publisher = publisher;
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
                    services.AddFusionWebSocketServer();
                })
                .ConfigureWebHost(builder => {
                    builder.UseKestrel().UseUrls(BaseUri.ToString());
                    builder.Configure((ctx, app) => {
                        app.UseFusionWebSocketServer(true);
                    });
                })
                .Build();
            await host.StartAsync().ConfigureAwait(false);

            // ReSharper disable once HeapView.BoxingAllocation
            return AsyncDisposable.New(async host1 => {
                await host1.StopAsync().ConfigureAwait(false);
            }, host);
        }
    }
}
