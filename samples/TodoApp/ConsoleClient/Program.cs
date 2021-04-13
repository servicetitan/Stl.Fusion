using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client;
using Templates.TodoApp.Abstractions;
using Templates.TodoApp.Abstractions.Clients;
using static System.Console;

Write("Enter SessionId to use: ");
var sessionId = ReadLine()!.Trim();
var session = new Session(sessionId);

var services = CreateServiceProvider();
var todoService = services.GetRequiredService<ITodoService>();
var computed = await Computed.Capture(ct => todoService.GetSummary(session, ct));
for (;;) {
    WriteLine($"- {computed.Value}");
    await computed.WhenInvalidated();
    computed = await computed.Update();
}

IServiceProvider CreateServiceProvider()
{
    // ReSharper disable once VariableHidesOuterVariable
    var services = new ServiceCollection();
    services.AddLogging(b => {
        b.ClearProviders();
        b.SetMinimumLevel(LogLevel.Warning);
        b.AddConsole();
    });

    var baseUri = new Uri("http://localhost:5005");
    var apiBaseUri = new Uri($"{baseUri}api/");

    var fusion = services.AddFusion();
    var fusionClient = fusion.AddRestEaseClient(
        (c, o) => {
            o.BaseUri = baseUri;
            o.MessageLogLevel = LogLevel.Information;
        }).ConfigureHttpClientFactory(
        (c, name, o) => {
            var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
            var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
            o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
        });
    fusionClient.AddReplicaService<ITodoService, ITodoClient>();
    fusion.AddAuthentication().AddRestEaseClient();

    // Default IUpdateDelayer
    services.AddSingleton<IUpdateDelayer>(_ => UpdateDelayer.Default with {
        UpdateDelayDuration = TimeSpan.FromSeconds(0.1)
    });
    return services.BuildServiceProvider();
}
