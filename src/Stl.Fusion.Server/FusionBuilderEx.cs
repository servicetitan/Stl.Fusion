using System;

namespace Stl.Fusion.Server
{
    public static class FusionBuilderEx
    {
        public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion,
            Action<IServiceProvider, FusionWebSocketServer.Options>? optionsBuilder = null)
            => new(fusion, optionsBuilder);

        public static FusionBuilder AddWebServer(this FusionBuilder fusion,
            Action<FusionWebServerBuilder> configureWebSocketServer,
            Action<IServiceProvider, FusionWebSocketServer.Options>? optionsBuilder = null)
        {
            var webSocketServer = fusion.AddWebServer(optionsBuilder);
            configureWebSocketServer.Invoke(webSocketServer);
            return fusion;
        }
    }
}
