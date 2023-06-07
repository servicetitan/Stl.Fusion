using Microsoft.Owin;
using Owin;

namespace Stl.Rpc.Server;

public static class EndpointRouteBuilderExt
{
    public static IAppBuilder MapRpcServer(
        this IAppBuilder appBuilder, IServiceProvider services, string? pattern = null)
    {
        if (appBuilder == null) throw new ArgumentNullException(nameof(appBuilder));
        if (services == null) throw new ArgumentNullException(nameof(services));

        var server = services.GetRequiredService<RpcServer>();

        return appBuilder.Map(pattern ?? server.Settings.RequestPath, app => {
            app.Run(delegate(IOwinContext context) {
                var statusCode = server.HandleRequest(context);
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            });
        });
    }
}
