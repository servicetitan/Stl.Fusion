using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Controllers;

namespace Stl.Fusion.Server
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddServer(this FusionAuthenticationBuilder fusionAuth,
            Action<IServiceProvider, FusionSessionMiddleware.Options>? sessionMiddlewareOptionsBuilder = null,
            Action<IServiceProvider, FusionAuthHelper.Options>? authHelperOptionsBuilder = null,
            Action<IServiceProvider, FusionSignInController.Options>? signInControllerOptionsBuilder = null)
        {
            var fusion = fusionAuth.Fusion;
            var services = fusionAuth.Services;
            fusionAuth.AddServerSideAuthService();

            services.TryAddSingleton(c => {
                var options = new FusionSessionMiddleware.Options();
                sessionMiddlewareOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddScoped<FusionSessionMiddleware>();
            services.TryAddSingleton(c => {
                var options = new FusionAuthHelper.Options();
                authHelperOptionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddScoped<FusionAuthHelper>();

            services.AddRouting();
            fusion.AddWebServer().AddControllers(signInControllerOptionsBuilder);

            return fusionAuth;
        }
    }
}
