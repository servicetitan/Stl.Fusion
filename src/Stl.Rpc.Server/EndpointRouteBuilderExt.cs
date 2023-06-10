using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Stl.Rpc.Server;

public static class EndpointRouteBuilderExt
{
    public static IEndpointConventionBuilder MapRpcServer(
        this IEndpointRouteBuilder endpoints, string? pattern = null)
    {
        var services = endpoints.ServiceProvider;
        var server = services.GetRequiredService<RpcWebSocketServer>();
        return endpoints
            .MapGet(pattern ?? server.Settings.RequestPath, ctx => server.HandleRequest(ctx))
            .WithDisplayName(server.GetType().FullName!);
    }
}
