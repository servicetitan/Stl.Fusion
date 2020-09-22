using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor.Authentication;

namespace Stl.Fusion.Blazor
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddBlazor(
            this FusionAuthenticationBuilder fusionAuth,
            Action<AuthorizationOptions>? configure = null)
        {
            var services = fusionAuth.Services;
            services.AddAuthorizationCore(configure);
            services.RemoveAll(typeof(AuthenticationStateProvider));
            services.TryAddScoped<AuthenticationStateProvider, AuthStateProvider>();
            services.TryAddTransient(c => (AuthStateProvider) c.GetRequiredService<AuthenticationStateProvider>());
            return fusionAuth;
        }

        public static FusionAuthenticationBuilder AddServerSideBlazor(
            this FusionAuthenticationBuilder fusionAuth,
            Action<AuthorizationOptions>? configure = null)
        {
            fusionAuth.Services.RemoveAll(typeof(AuthenticationStateProvider));
            return fusionAuth.AddBlazor();
        }
    }
}
