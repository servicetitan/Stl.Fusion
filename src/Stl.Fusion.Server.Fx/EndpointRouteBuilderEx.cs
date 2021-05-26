//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Routing;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;

namespace Stl.Fusion.Server
{
    public static class EndpointRouteBuilderEx
    {
        //public static IEndpointConventionBuilder MapFusionWebSocketServer(
        //    this IEndpointRouteBuilder endpoints, string? pattern = null)
        //{
        //    var services = endpoints.ServiceProvider;
        //    var server = services.GetRequiredService<WebSocketServer>();
        //    return endpoints
        //        .MapGet(pattern ?? server.RequestPath, ctx => server.HandleRequest(ctx))
        //        .WithDisplayName("Stl.Fusion WebSocket Server");
        //}

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
}
