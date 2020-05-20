using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Extensibility;
using Stl.Hosting;

namespace Stl.Fusion.Client
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionWebSocketClient(
            this IServiceCollection services, 
            WebSocketClient.Options options, 
            bool addTransient = false)
        {
            services.TryAddSingleton(options);
            if (!services.HasService<WebSocketClient>()) {
                if (addTransient)
                    services.AddTransient<WebSocketClient>();
                else
                    services.AddAsyncProcessSingleton<WebSocketClient>();
            }
            return services;
        }

        public static IServiceCollection AddFusionWebSocketClient(
            this IServiceCollection services, 
            Action<IServiceProvider, WebSocketClient.Options>? optionsBuilder = null, 
            bool addTransient = false)
        {
            services.TryAddSingleton(c => {
                var options = new WebSocketClient.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            if (!services.HasService<WebSocketClient>()) {
                if (addTransient)
                    services.AddTransient<WebSocketClient>();
                else
                    services.AddAsyncProcessSingleton<WebSocketClient>();
            }
            return services;
        }        
    }
}
