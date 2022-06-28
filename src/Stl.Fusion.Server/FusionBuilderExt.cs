namespace Stl.Fusion.Server;

public static class FusionBuilderExt
{
    public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion,
        Func<IServiceProvider, WebSocketServer.Options>? optionsFactory = null)
        => new(fusion, optionsFactory);

    public static FusionBuilder AddWebServer(this FusionBuilder fusion,
        Action<FusionWebServerBuilder> configureWebServer,
        Func<IServiceProvider, WebSocketServer.Options>? optionsFactory = null)
    {
        var fusionWebServer = fusion.AddWebServer(optionsFactory);
        configureWebServer(fusionWebServer);
        return fusion;
    }
}
