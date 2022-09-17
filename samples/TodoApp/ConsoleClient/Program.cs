using Stl.Fusion.Client;
using Stl.Fusion.UI;
using Templates.TodoApp.Abstractions;
using Templates.TodoApp.Abstractions.Clients;
using static System.Console;

Write("Enter SessionId to use: ");
var sessionId = ReadLine()!.Trim();
var session = new Session(sessionId);

var services = CreateServiceProvider();
var todoService = services.GetRequiredService<ITodoService>();
var computed = await Computed.Capture(() => todoService.GetSummary(session));
await foreach (var c in computed.Changes()) {
    WriteLine($"- {c.Value}");
}

IServiceProvider CreateServiceProvider()
{
    // ReSharper disable once VariableHidesOuterVariable
    var services = new ServiceCollection();
    services.AddLogging(logging => {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Warning);
        logging.AddConsole();
    });

    var baseUri = new Uri("http://localhost:5005");
    var apiBaseUri = new Uri($"{baseUri}api/");

    var fusion = services.AddFusion();
    fusion.AddRestEaseClient(
        client => {
            client.ConfigureWebSocketChannel(_ => new() { BaseUri = baseUri });
            client.ConfigureHttpClient((_, name, o) => {
                var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                o.HttpClientActions.Add(httpClient => httpClient.BaseAddress = clientBaseUri);
            });
            client.AddReplicaService<ITodoService, ITodoClientDef>();
        });
    fusion.AddAuthentication().AddRestEaseClient();

    // Default update delay is 0.2s
    services.AddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.2));

    return services.BuildServiceProvider();
}
