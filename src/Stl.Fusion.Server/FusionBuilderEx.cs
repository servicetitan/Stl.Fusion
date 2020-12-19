using System;

namespace Stl.Fusion.Server
{
    public static class FusionBuilderEx
    {
        public static FusionWebSocketServerBuilder AddWebSocketServer(this FusionBuilder fusion,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
            => new(fusion, optionsBuilder);

        public static FusionBuilder AddWebSocketServer(this FusionBuilder fusion,
            Action<FusionWebSocketServerBuilder> configureWebSocketServer,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
        {
            var webSocketServer = fusion.AddWebSocketServer(optionsBuilder);
            configureWebSocketServer.Invoke(webSocketServer);
            return fusion;
        }
    }
}
