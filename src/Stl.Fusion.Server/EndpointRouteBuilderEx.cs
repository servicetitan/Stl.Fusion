using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Server
{
    public static class EndpointRouteBuilderEx
    {
        public static IEndpointConventionBuilder MapFusionWebSocketServer(
            this IEndpointRouteBuilder endpoints, string? pattern = null)
        {
            var services = endpoints.ServiceProvider;
            var server = services.GetRequiredService<FusionWebSocketServer>();
            return endpoints
                .MapGet(pattern ?? server.RequestPath, ctx => server.HandleAsync(ctx))
                .WithDisplayName("Stl.Fusion WebSocket Server");
        }
    }
}
