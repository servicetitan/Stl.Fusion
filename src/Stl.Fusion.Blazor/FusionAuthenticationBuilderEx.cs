using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor.Authentication;
using Stl.Fusion.Client;

namespace Stl.Fusion.Blazor
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddBlazorCore(this FusionAuthenticationBuilder fusionAuth)
        {
            var services = fusionAuth.Services;
            services.TryAddScoped<AuthenticationStateProvider, AuthStateProvider>();
            return fusionAuth;
        }

        public static FusionAuthenticationBuilder AddBlazorClient(this FusionAuthenticationBuilder fusionAuth)
            => fusionAuth.AddBlazorCore().AddClient();
    }
}
