using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Stl.Fusion.Server;

public static class EndpointRouteBuilderExt
{
    public static IEndpointConventionBuilder MapFusionWebSocketServer(
        this IEndpointRouteBuilder endpoints, string? pattern = null)
    {
        var services = endpoints.ServiceProvider;
        var server = services.GetRequiredService<WebSocketServer>();
        return endpoints
            .MapGet(pattern ?? server.Settings.RequestPath, ctx => server.HandleRequest(ctx))
            .WithDisplayName("Stl.Fusion WebSocket Server");
    }
}
