using System;
using System.Reflection;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Fusion.Client;
using Stl.OS;
using Stl.DependencyInjection;
using Stl.Fusion.Blazor;
using Stl.Fusion.Client.Internal;
using Stl.Fusion.Extensions;
using Templates.Blazor2.Abstractions;
using Templates.Blazor2.Abstractions.Clients;
using Templates.Blazor2.Services;

namespace Templates.Blazor2.UI
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ConfigureServices(builder.Services, builder);
            builder.RootComponents.Add<App>("#app");
            var host = builder.Build();

            host.Services.HostedServices().Start();
            return host.RunAsync();
        }

        public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.Logging.AddFilter(typeof(App).Namespace, LogLevel.Information);

            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            var apiBaseUri = new Uri($"{baseUri}api/");

            // Fusion services
            var fusion = services.AddFusion();
            var fusionClient = fusion.AddRestEaseClient((_, o) => {
                o.BaseUri = baseUri;
                o.MessageLogLevel = LogLevel.Information;
            });
            fusionClient.ConfigureHttpClientFactory((c, name, o) => {
                var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
            });
            fusion.AddAuthentication().AddRestEaseClient().AddBlazor();

            // Option 1: Client-side SimpleTodoService (no RPC)
            // fusion.AddComputeService<ITodoService, SimpleTodoService>();

            // Option 2: Client-side TodoService + IKeyValueStoreClient (remote IKeyValueStore)
            // fusionClient.AddReplicaService<IKeyValueStore, IKeyValueStoreClient>();
            // fusion.AddComputeService<ITodoService, TodoService>();

            // Option 3: ITodoClient (remote ITodoService)
            fusionClient.AddReplicaService<ITodoService, ITodoClient>();

            ConfigureSharedServices(services);
        }

        public static void ConfigureSharedServices(IServiceCollection services)
        {
            services.AddBlazorise().AddBootstrapProviders().AddFontAwesomeIcons();

            // Default delay for update delayers
            services.AddSingleton(c => new UpdateDelayer.Options() {
                DelayDuration = TimeSpan.FromSeconds(0.1),
            });

            // Extensions
            var fusion = services.AddFusion();
            fusion.AddLiveClock();
        }
    }
}
