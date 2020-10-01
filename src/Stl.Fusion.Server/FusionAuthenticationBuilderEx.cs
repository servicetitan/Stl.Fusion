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
            Action<IServiceProvider, SessionMiddleware.Options>? sessionMiddlewareOptionsBuilder = null,
            Type? authServiceImplementationType = null)
        {
            fusionAuth.AddAuthService(authServiceImplementationType);

            var services = fusionAuth.Services;
            services.TryAddSingleton(c => {
                var options = new SessionMiddleware.Options();
                sessionMiddlewareOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddScoped<SessionMiddleware>();
            services.AddRouting();
            services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);

            return fusionAuth;
        }
    }
}
