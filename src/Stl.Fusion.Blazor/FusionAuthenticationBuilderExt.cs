using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor
{
    public static class FusionAuthenticationBuilderExt
    {
        public static FusionAuthenticationBuilder AddBlazor(
            this FusionAuthenticationBuilder fusionAuth,
            Action<AuthorizationOptions>? configure = null)
        {
            configure ??= _ => {}; // .NET 5.0 doesn't allow to pass null to AddAuthorizationCore
            var services = fusionAuth.Services;
            if (services.HasService<ClientAuthHelper>())
                return fusionAuth;

            fusionAuth.Fusion.AddBlazorUIServices();
            services.AddAuthorizationCore(configure);
            services.RemoveAll(typeof(AuthenticationStateProvider));
            services.TryAddSingleton<AuthStateProvider.Options>();
            services.TryAddScoped<AuthenticationStateProvider, AuthStateProvider>();
            services.TryAddTransient(c => (AuthStateProvider) c.GetRequiredService<AuthenticationStateProvider>());
            services.TryAddScoped<ClientAuthHelper>();
            return fusionAuth;
        }
    }
}
