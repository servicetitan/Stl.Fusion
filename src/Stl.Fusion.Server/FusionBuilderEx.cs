using System;

namespace Stl.Fusion.Server
{
    public static class FusionBuilderEx
    {
        public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
            => new(fusion, optionsBuilder);

        public static FusionBuilder AddWebServer(this FusionBuilder fusion,
            Action<FusionWebServerBuilder> configureWebServer,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
        {
            var fusionWebServer = fusion.AddWebServer(optionsBuilder);
            configureWebServer.Invoke(fusionWebServer);
            return fusion;
        }
    }
}
