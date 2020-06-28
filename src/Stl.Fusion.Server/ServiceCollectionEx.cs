using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.Server
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionWebSocketServer(this IServiceCollection services,
            WebSocketServer.Options options)
        {
            services.TryAddSingleton(options);
            services.TryAddSingleton<WebSocketServer>();
            return services.AddFusionServerCore();
        }

        public static IServiceCollection AddFusionWebSocketServer(this IServiceCollection services,
            Action<IServiceProvider, WebSocketServer.Options>? optionsBuilder = null)
        {
            services.TryAddSingleton(c => {
                var options = new WebSocketServer.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddSingleton<WebSocketServer>();
            return services.AddFusionServerCore();
        }
    }
}
