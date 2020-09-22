using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor.Authentication;
using Stl.Fusion.Client;

namespace Stl.Fusion.Blazor
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddBlazorCore(
            this FusionAuthenticationBuilder fusionAuth,
            Action<AuthorizationOptions>? configure = null)
        {
            var services = fusionAuth.Services;
            services.AddAuthorizationCore(configure);
            services.TryAddScoped<AuthenticationStateProvider, AuthStateProvider>();
            services.TryAddTransient(c => (AuthStateProvider) c.GetRequiredService<AuthenticationStateProvider>());
            return fusionAuth;
        }

        public static FusionAuthenticationBuilder AddBlazorClient(
            this FusionAuthenticationBuilder fusionAuth,
            Action<AuthorizationOptions>? configure = null)
            => fusionAuth.AddBlazorCore(configure).AddClient();
    }
}
