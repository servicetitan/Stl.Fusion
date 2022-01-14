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
        Func<IServiceProvider, SessionMiddleware.Options>? sessionMiddlewareOptionsFactory = null,
        Func<IServiceProvider, ServerAuthHelper.Options>? serverAuthHelperSettingsFactory = null,
        Func<IServiceProvider, SignInController.Options>? signInControllerSettingsFactory = null)
    {
        var fusion = fusionAuth.Fusion;
        var services = fusionAuth.Services;
        fusionAuth.AddAuthBackend();

        services.TryAddSingleton(c => sessionMiddlewareOptionsFactory?.Invoke(c) ?? SessionMiddleware.DefaultSettings);
        services.TryAddScoped<SessionMiddleware>();
        services.TryAddSingleton(c => serverAuthHelperSettingsFactory?.Invoke(c) ?? ServerAuthHelper.DefaultSettings);
        services.TryAddScoped<ServerAuthHelper>();
        services.TryAddSingleton<AuthSchemasCache>();

        services.AddRouting();
        fusion.AddWebServer().AddControllers(signInControllerSettingsFactory);

        return fusionAuth;
    }
}
