using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.Server
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionWebSocketServer(this IServiceCollection services, 
            WebSocketServerMiddleware.Options options)
        {
            services.TryAddSingleton(options);
            services.TryAddScoped<WebSocketServerMiddleware>();
            return services;
        }

        public static IServiceCollection AddFusionWebSocketServer(this IServiceCollection services, 
            Action<WebSocketServerMiddleware.Options>? optionsBuilder = null)
        {
            var options = new WebSocketServerMiddleware.Options();
            optionsBuilder?.Invoke(options);
            return services.AddFusionWebSocketServer(options);
        }        
    }
}
