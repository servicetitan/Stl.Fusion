using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor.Authentication;

public static class FusionBlazorBuilderExt
{
    public static FusionBlazorBuilder AddAuthentication(
        this FusionBlazorBuilder fusionBlazor,
        Action<AuthorizationOptions>? configure = null)
    {
        var services = fusionBlazor.Services;
        if (services.HasService<ClientAuthHelper>())
            return fusionBlazor;

        services.AddAuthorizationCore(configure ?? (_ => {})); // .NET 5.0 doesn't allow to pass null here
        services.RemoveAll(typeof(AuthenticationStateProvider));
        services.TryAddSingleton(_ => new AuthStateProvider.Options());
        services.TryAddScoped<AuthenticationStateProvider>(c => new AuthStateProvider(
            c.GetRequiredService<AuthStateProvider.Options>(), c));
        services.TryAddScoped(c => (AuthStateProvider)c.GetRequiredService<AuthenticationStateProvider>());
        services.TryAddScoped(c => new ClientAuthHelper(c));
        return fusionBlazor;
    }

    public static FusionBlazorBuilder AddPresenceReporter(
        this FusionBlazorBuilder fusionBlazor,
        Func<IServiceProvider, PresenceReporter.Options>? optionsFactory = null)
    {
        var services = fusionBlazor.Services;
        services.AddSingleton(optionsFactory, _ => PresenceReporter.Options.Default);
        if (services.HasService<PresenceReporter>())
            return fusionBlazor;

        services.TryAddScoped(c => new PresenceReporter(c.GetRequiredService<PresenceReporter.Options>(), c));
        return fusionBlazor;
    }
}
