namespace Stl.Fusion.Server;

public static class FusionBuilderExt
{
    public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion,
        Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
        => new(fusion, optionsBuilder);

    public static FusionBuilder AddWebServer(this FusionBuilder fusion,
        Action<FusionWebServerBuilder> configureWebSocketServer,
        Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
    {
        var webSocketServer = fusion.AddWebServer(optionsBuilder);
        configureWebSocketServer(webSocketServer);
        return fusion;
    }
}
