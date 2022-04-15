using Microsoft.Owin;
using Owin;

namespace Stl.Fusion.Server;

public static class EndpointRouteBuilderExt
{
    public static IAppBuilder MapFusionWebSocketServer(
        this IAppBuilder appBuilder, IServiceProvider services, string? pattern = null)
    {
        if (appBuilder == null) throw new ArgumentNullException(nameof(appBuilder));
        if (services == null) throw new ArgumentNullException(nameof(services));

        var server = services.GetRequiredService<WebSocketServer>();

        return appBuilder.Map(pattern ?? server.RequestPath, app => {
            app.Run(delegate(IOwinContext ctx) {
                var statusCode = server.HandleRequest(ctx);
                ctx.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            });
        });
    }
}
