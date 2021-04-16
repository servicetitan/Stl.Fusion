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
using Templates.TodoApp.Abstractions;
using Templates.TodoApp.Abstractions.Clients;
using Templates.TodoApp.Services;

namespace Templates.TodoApp.UI
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

            // Option 2: Client-side TodoService and SandboxedKeyValueStore using InMemoryKeyValueStore (no RPC)
            // fusion.AddInMemoryKeyValueStore();
            // fusion.AddSandboxedKeyValueStore();
            // fusion.AddComputeService<ITodoService, TodoService>();

            // Option 3: Client-side TodoService + remote SandboxedKeyValueStore -> DbKeyValueStore
            // fusionClient.AddReplicaService<ISandboxedKeyValueStore, ISandboxedKeyValueStoreClient>();
            // fusion.AddComputeService<ITodoService, TodoService>();

            // Option 4: Remote TodoService, SandboxedKeyValueStore, and DbKeyValueStore
            fusionClient.AddReplicaService<ITodoService, ITodoClient>();

            ConfigureSharedServices(services);
        }

        public static void ConfigureSharedServices(IServiceCollection services)
        {
            services.AddBlazorise().AddBootstrapProviders().AddFontAwesomeIcons();

            // Default IUpdateDelayer
            services.AddSingleton<IUpdateDelayer>(_ => new UpdateDelayer(0.5));

            // Extensions
            var fusion = services.AddFusion();
            fusion.AddFusionTime();
        }
    }
}
