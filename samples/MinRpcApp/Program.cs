using Samples.MiniRpcApp;
using Stl.CommandR;
using Stl.Fusion;
using Stl.Fusion.Server;
using Stl.Rpc;
using Stl.Rpc.Server;
using static System.Console;

#pragma warning disable ASP0000

var baseUrl = "http://localhost:22222/";
await (args switch {
    [ "server" ] => RunServer(),
    [ "client" ] => RunClient(),
    _ => RunBoth(),
});

async Task RunServer()
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders().AddDebug();
    builder.Services.AddFusion(RpcServiceMode.Server, fusion => {
        fusion.AddWebServer();
        fusion.AddService<IChat, Chat>();
    });
    var app = builder.Build();

    app.UseWebSockets();
    app.MapRpcWebSocketServer();
    await app.RunAsync(baseUrl);
}

async Task RunClient()
{
    var services = new ServiceCollection()
        .AddFusion(fusion => {
            fusion.Rpc.AddWebSocketClient(baseUrl);
            fusion.AddClient<IChat>();
        })
        .BuildServiceProvider();

    var chat = services.GetRequiredService<IChat>();
    var commander = services.Commander();
    _ = Task.Run(ObserveMessages);
    _ = Task.Run(ObserveMessageCount);
    while (true) {
        var message = ReadLine() ?? "";
        try {
            await commander.Call(new Chat_Post(message));
        }
        catch (Exception error) {
            WriteLine($"{error.GetType()}: {error.Message}");
        }
    }

    async Task ObserveMessages() {
        var cMessages = await Computed.Capture(() => chat.GetRecentMessages());
        await foreach (var (messages, _, version) in cMessages.Changes()) {
            WriteLine($"Messages changed (version: {version}):");
            foreach (var message in messages)
                WriteLine($"- {message}");
        }
    };

    async Task ObserveMessageCount() {
        var cMessageCount = await Computed.Capture(() => chat.GetMessageCount());
        await foreach (var (count, _) in cMessageCount.Changes())
            WriteLine($"Message count changed: {count}");
    };
}

Task RunBoth()
    => Task.WhenAll(RunServer(), RunClient());
