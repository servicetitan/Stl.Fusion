using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Hosting;
using Stl.Samples.Blazor.Services;
using Stl.Serialization;

namespace Stl.Samples.Blazor
{
    public class ClientProgram
    {
        public Task RunAsync(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ConfigureServices(builder.Services);
            // builder.Configuration.Add(new MemoryConfigurationSource() {
            //     InitialData = new Dictionary<string, string>() {
            //         { "ASPNETCORE_ENVIRONMENT", "Development" },
            //     } 
            // });
            builder.Services.AddTransient(sp => new HttpClient {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
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

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddDebug());
            services.AddFusion();
            services.AddAsyncProcessSingleton<Client>();
        }
    }
}
