using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Controllers;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

public static class FusionAuthenticationBuilderExt
{
    public static FusionAuthenticationBuilder AddServer(this FusionAuthenticationBuilder fusionAuth,
        Func<IServiceProvider, SessionMiddleware.Options>? sessionMiddlewareOptionsFactory = null,
        Func<IServiceProvider, ServerAuthHelper.Options>? serverAuthHelperOptionsFactory = null,
        Func<IServiceProvider, SignInController.Options>? signInControllerOptionsFactory = null)
    {
        var fusion = fusionAuth.Fusion;
        var services = fusionAuth.Services;
        fusionAuth.AddBackend();

        services.TryAddSingleton(c => sessionMiddlewareOptionsFactory?.Invoke(c) ?? new());
        services.TryAddScoped<SessionMiddleware>();
        services.TryAddSingleton(c => serverAuthHelperOptionsFactory?.Invoke(c) ?? new());
        services.TryAddScoped<ServerAuthHelper>();
        services.TryAddSingleton<AuthSchemasCache>();

        services.AddRouting();
        fusion.AddWebServer().AddControllers(signInControllerOptionsFactory);

        return fusionAuth;
    }
}
