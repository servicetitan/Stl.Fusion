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
using Stl.Samples.Blazor.Client.UI;

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
            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = baseUri;
                o.MessageLogLevel = LogLevel.Information;
            });
            
            // Replica services
            var apiUri = new Uri($"{baseUri}api/");
            services.AddSingleton(new HttpClient() { BaseAddress = apiUri });
            services.AddReplicaService<ITimeClient>("time");
            services.AddReplicaService<IScreenshotClient>("screenshot");
            services.AddReplicaService<IChatClient>("chat");

            // Configuring live updaters
            services.AddSingleton(c => new UpdateDelayer.Options() {
                Delay = TimeSpan.FromSeconds(0.05),
            });
            services.AddAllLive(typeof(Program).Assembly, (c, options) => {
                if (options is Live<ServerTimeUI>.Options) {
                    options.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                        Delay = TimeSpan.FromSeconds(0.5),
                    });
                }
            });
        }
    }
}
