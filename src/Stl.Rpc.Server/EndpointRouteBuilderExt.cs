using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Stl.Rpc.Server;

public static class EndpointRouteBuilderExt
{
    public static IEndpointRouteBuilder MapRpcWebSocketServer(
        this IEndpointRouteBuilder endpoints, string? pattern = null)
    {
        var services = endpoints.ServiceProvider;
        var server = services.GetRequiredService<RpcWebSocketServer>();
        endpoints
            .MapGet(pattern ?? server.Settings.RoutePattern, server.Invoke)
            .WithDisplayName(server.GetType().FullName!);
        return endpoints;
    }
}
