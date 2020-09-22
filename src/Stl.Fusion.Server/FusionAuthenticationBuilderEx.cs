using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Authentication;

namespace Stl.Fusion.Server
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddServer(this FusionAuthenticationBuilder fusionAuth,
            Type? authServiceImplementationType = null)
        {
            fusionAuth.AddAuthService(authServiceImplementationType);

            var services = fusionAuth.Services;
            services.TryAddSingleton<AuthContextMiddleware.Options>();
            services.TryAddScoped<AuthContextMiddleware>();
            services.AddRouting();
            services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);

            return fusionAuth;
        }
    }
}
