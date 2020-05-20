using Microsoft.AspNetCore.Builder;

namespace Stl.Fusion.Server
{
    public static class ApplicationBuilderEx
    {
        public static IApplicationBuilder UseFusionWebSocketServer(
            this IApplicationBuilder app, bool addUseWebSockets = false)
        {
            if (addUseWebSockets)
                app.UseWebSockets(new WebSocketOptions() {
                    ReceiveBufferSize = 16_384,
                });
            return app.UseMiddleware<WebSocketServerMiddleware>();
        }
    }
}
