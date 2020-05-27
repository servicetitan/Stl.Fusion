using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.OS;
using Stl.Reflection;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.Wasm)
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
            if (baseUri.Port != Settings.ServerPort)
                // It's a dev server, let's replace the port there
                baseUri = new UriBuilder(baseUri) {Port = Settings.ServerPort}.Uri;

            services.AddLogging(logging => logging.AddDebug());
            services.AddFusion();
            services.AddFusionWebSocketChannelProvider((c, o) => {
                o.BaseUri = baseUri;
            });
            services.AddTransient(c => new HttpClient { BaseAddress = baseUri });
        }
    }
}
