using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.Blazor;

public static class FusionAuthenticationBuilderExt
{
    public static FusionBuilder AddBlazorAuthentication(
        this FusionBuilder fusion,
        Action<AuthorizationOptions>? configure = null)
    {
        configure ??= _ => {}; // .NET 5.0 doesn't allow to pass null to AddAuthorizationCore
        var services = fusion.Services;
        if (services.HasService<ClientAuthHelper>())
            return fusion;

        fusion.AddBlazorUIServices();
        services.AddAuthorizationCore(configure);
        services.RemoveAll(typeof(AuthenticationStateProvider));
        services.TryAddSingleton(_ => new AuthStateProvider.Options());
        services.TryAddScoped<AuthenticationStateProvider>(c => new AuthStateProvider(
            c.GetRequiredService<AuthStateProvider.Options>(), c));
        services.TryAddScoped(c => (AuthStateProvider)c.GetRequiredService<AuthenticationStateProvider>());
        services.TryAddScoped(c => new ClientAuthHelper(c));
        return fusion;
    }
}
