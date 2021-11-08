using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Controllers;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

public static class FusionAuthenticationBuilderExt
{
    public static FusionAuthenticationBuilder AddServer(this FusionAuthenticationBuilder fusionAuth,
        Action<IServiceProvider, SessionMiddleware.Options>? sessionMiddlewareOptionsBuilder = null,
        Action<IServiceProvider, ServerAuthHelper.Options>? authHelperOptionsBuilder = null,
        Action<IServiceProvider, SignInController.Options>? signInControllerOptionsBuilder = null)
    {
        var fusion = fusionAuth.Fusion;
        var services = fusionAuth.Services;
        fusionAuth.AddAuthBackend();

        services.TryAddSingleton(c => {
            var options = new SessionMiddleware.Options();
            sessionMiddlewareOptionsBuilder?.Invoke(c, options);
            return options;
        });
        services.TryAddScoped<SessionMiddleware>();
        services.TryAddSingleton(c => {
            var options = new ServerAuthHelper.Options();
            authHelperOptionsBuilder?.Invoke(c, options);
            return options;
        });
        services.TryAddSingleton<AuthSchemasCache>();
        services.TryAddScoped<ServerAuthHelper>();

        services.AddRouting();
        fusion.AddWebServer().AddControllers(signInControllerOptionsBuilder);

        return fusionAuth;
    }
}
