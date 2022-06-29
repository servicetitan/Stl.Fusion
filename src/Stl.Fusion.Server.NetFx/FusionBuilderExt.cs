namespace Stl.Fusion.Server;

public static class FusionBuilderExt
{
    public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion,
        Func<IServiceProvider, WebSocketServer.Options>? optionsFactory = null)
        => new(fusion, optionsFactory);

    public static FusionBuilder AddWebServer(this FusionBuilder fusion,
        Action<FusionWebServerBuilder> configureWebSocketServer,
        Func<IServiceProvider, WebSocketServer.Options>? optionsFactory = null)
    {
        var webSocketServer = fusion.AddWebServer(optionsFactory);
        configureWebSocketServer(webSocketServer);
        return fusion;
    }
}
