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
            services.AddHttpContextAccessor();
            services.TryAddSingleton(options);
            services.TryAddScoped<WebSocketServerMiddleware>();
            return services;
        }

        public static IServiceCollection AddFusionWebSocketServer(this IServiceCollection services, 
            Action<IServiceProvider, WebSocketServerMiddleware.Options>? optionsBuilder = null)
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton(c => {
                var options = new WebSocketServerMiddleware.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddScoped<WebSocketServerMiddleware>();
            return services;
        }        
    }
}
