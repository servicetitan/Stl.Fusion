using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Hosting;
using Stl.OS;
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
            ConfigureServices(builder.Services);
            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            if (baseUri.Port != Settings.ServerPort)
                // It's a dev server, let's replace the port there
                baseUri = new UriBuilder(baseUri) {Port = Settings.ServerPort}.Uri;
            builder.Services.AddTransient(sp => {
                var client = new HttpClient {
                    BaseAddress = baseUri,
                };
                return client;
            });
            builder.RootComponents.Add<App>("app");
            var host = builder.Build();
            var runTask = host.RunAsync();
            Task.Delay(1000).ContinueWith(_ => {
                var hostedServices = host.Services.GetService<IEnumerable<IHostedService>>();
                foreach (var hostedService in hostedServices)
                    hostedService.StartAsync(default);
            });
            return runTask;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddDebug());
            services.AddFusion();
            services.AddAsyncProcessSingleton<Client>();
        }
    }
}
