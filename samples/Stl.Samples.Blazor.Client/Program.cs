using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestEase.Implementation;
using Stl.Extensibility;
using Stl.Fusion.Client;
using Stl.Fusion.UI;
using Stl.OS;
using Stl.Samples.Blazor.Client.Services;

namespace Stl.Samples.Blazor.Client
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ConfigureServices(builder.Services, builder);
            builder.RootComponents.Add<App>("app");
            var host = builder.Build();
            
            var runTask = host.RunAsync();
            Task.Run(async () => {
                var hostedServices = host.Services.GetService<IEnumerable<IHostedService>>();
                foreach (var hostedService in hostedServices)
                    await hostedService.StartAsync(default);
            });
            return runTask;
        }

        private static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            HttpClientHandler CreateNoCacheHandler() =>
                new ModifyingClientHttpHandler((requestMessage, cancellationToken) => {
                    requestMessage.SetBrowserRequestCache(BrowserRequestCache.NoStore);
                    return Task.CompletedTask;
                });

            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            var apiUri = new Uri($"{baseUri}api");
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = baseUri;
                o.MessageLogLevel = LogLevel.Information;
            });
            
            // Replica services
            services.AddSingleton(c => Special.Use(new HttpClient(CreateNoCacheHandler()) {
                BaseAddress = new Uri($"{apiUri}/Time")
            }).For<ITimeClient>());
            services.AddReplicaService<ITimeClient>();
            services.AddSingleton(c => Special.Use(new HttpClient(CreateNoCacheHandler()) {
                BaseAddress = new Uri($"{apiUri}/Screenshot")
            }).For<IScreenshotClient>());
            services.AddReplicaService<IScreenshotClient>();
            services.AddSingleton(c => Special.Use(new HttpClient(CreateNoCacheHandler()) {
                BaseAddress = new Uri($"{apiUri}/Chat")
            }).For<IChatClient>());
            services.AddReplicaService<IChatClient>();

            // Live UI models
            services.AddLive<ServerTimeUI.Model, ServerTimeUI>((c, o) => {
                o.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                    Delay = TimeSpan.FromSeconds(0.5),
                });
            });
            services.AddLive<ServerScreenUI.Model, ServerScreenUI>((c, o) => {
                o.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                    Delay = TimeSpan.FromSeconds(0.01),
                });
            });
            services.AddLive<ChatUI.Model, ChatUI>((c, o) => {
                o.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                    Delay = TimeSpan.FromSeconds(0.01),
                });
            });
        }
    }
}
