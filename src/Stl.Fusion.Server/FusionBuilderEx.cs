using System;

namespace Stl.Fusion.Server
{
    public static class FusionBuilderEx
    {
        public static FusionWebSocketServerBuilder AddWebSocketServer(this FusionBuilder fusion,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
        {
            var builder = new FusionWebSocketServerBuilder(fusion);
            if (optionsBuilder != null)
                builder.ConfigureWebSocketServer(optionsBuilder);
            return builder;
        }
    }
}
