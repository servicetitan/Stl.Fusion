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
using Stl.Fusion;
using Stl.Fusion.Client;
using Stl.Fusion.UI;
using Stl.OS;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Client.UI;
using Stl.Samples.Blazor.Common.Services;

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
                // o.MessageLogLevel = LogLevel.Information;
            });

            // Computed services
            services.AddComputedService<IComposerService, ClientSideComposerService>();

            // Replica services
            var apiBaseUri = new Uri($"{baseUri}api/");
            services.AddTransient(c => new HttpClient() { BaseAddress = apiBaseUri });
            services.AddReplicaService<ITimeClient>("time");
            services.AddReplicaService<IScreenshotClient>("screenshot");
            services.AddReplicaService<IChatClient>("chat");
            services.AddReplicaService<IComposerClient>("composer");

            // Configuring live updaters
            services.AddSingleton(c => new UpdateDelayer.Options() {
                Delay = TimeSpan.FromSeconds(0.1),
            });
            services.AutoAddLiveState(typeof(Program).Assembly, (c, options) => {
                if (options is LiveState<ServerScreenState.Local, ServerScreenState>.Options) {
                    // Server Screen part always updates as quickly as it can
                    options.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                        Delay = TimeSpan.FromSeconds(0),
                    });
                }
                if (options is LiveState<ServerTimeState>.Options) {
                    // Slower auto-updates for Server Time part
                    options.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                        Delay = TimeSpan.FromSeconds(0.5),
                    });
                }
                if (options is LiveState<CompositionState.Local, CompositionState>.Options) {
                    // Slower auto-updates for Composition part
                    options.UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                        Delay = TimeSpan.FromSeconds(0.5),
                    });
                }
            });
        }
    }
}
