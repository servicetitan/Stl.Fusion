using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Stl.Fusion.Blazor;
using Stl.Fusion.Blazor.Authentication;
using Stl.Fusion.Diagnostics;
using Stl.Fusion.Extensions;
using Stl.Fusion.Client.Interception;
using Stl.Fusion.UI;
using Stl.OS;
using Stl.Rpc;
using Stl.Rpc.WebSockets;
using Templates.TodoApp.Abstractions;

namespace Templates.TodoApp.UI;

public static class StartupHelper
{
    public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddFilter(typeof(App).Namespace, LogLevel.Information);
        builder.Logging.AddFilter(typeof(FusionMonitor).Namespace, LogLevel.Information);
        builder.Logging.AddFilter(typeof(RpcHub).Namespace, LogLevel.Information);
        builder.Logging.AddFilter(typeof(WebSocketChannel<>).Namespace, LogLevel.Information);

        // Fusion services
        var fusion = services.AddFusion();
        fusion.Rpc.AddWebSocketClient(builder.HostEnvironment.BaseAddress);
        fusion.AddAuthClient();
        fusion.AddBlazor().AddAuthentication().AddPresenceReporter();

        // Option 1: Client-side SimpleTodoService (no RPC)
        // fusion.AddComputeService<ITodoService, SimpleTodoService>();

        // Option 2: Client-side TodoService and SandboxedKeyValueStore using InMemoryKeyValueStore (no RPC)
        // fusion.AddInMemoryKeyValueStore();
        // fusion.AddSandboxedKeyValueStore();
        // fusion.AddComputeService<ITodoService, TodoService>();

        // Option 3: Client-side TodoService + remote SandboxedKeyValueStore -> DbKeyValueStore
        // fusionClient.AddReplicaService<ISandboxedKeyValueStore, ISandboxedKeyValueStoreClientDef>();
        // fusion.AddComputeService<ITodoService, TodoService>();

        // Option 4: Remote TodoService, SandboxedKeyValueStore, and DbKeyValueStore
        fusion.AddClient<ITodoService>();

        ConfigureSharedServices(services);
    }

    public static void ConfigureSharedServices(IServiceCollection services)
    {
        IComputedState.DefaultOptions.MustFlowExecutionContext = true;

        // Blazorise
        services.AddBlazorise().AddBootstrapProviders().AddFontAwesomeIcons();

        // Other UI-related services
        var fusion = services.AddFusion();
        fusion.AddComputedGraphPruner(_ => new() { CheckPeriod = TimeSpan.FromSeconds(30) });
        fusion.AddFusionTime();
        // fusion.AddBackendStatus();

        // Default update delay is 0.5s
        services.AddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.5));

        // Diagnostics
        services.AddHostedService(c => {
            var isWasm = OSInfo.IsWebAssembly;
            return new FusionMonitor(c) {
                SleepPeriod = isWasm
                    ? TimeSpan.Zero
                    : TimeSpan.FromMinutes(1).ToRandom(0.25),
                CollectPeriod = TimeSpan.FromSeconds(isWasm ? 3 : 60),
                AccessFilter = isWasm
                    ? static computed => computed.Input.Function is IClientComputeMethodFunction
                    : static computed => true,
                AccessStatisticsPreprocessor = StatisticsPreprocessor,
                RegistrationStatisticsPreprocessor = StatisticsPreprocessor,
            };

            void StatisticsPreprocessor(Dictionary<string, (int, int)> stats)
            {
                foreach (var key in stats.Keys.ToList()) {
                    if (key.Contains(".Pseudo"))
                        stats.Remove(key);
                    if (key.StartsWith("FusionTime."))
                        stats.Remove(key);
                }
            }
        });
    }
}
