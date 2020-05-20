using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Extensibility;
using Stl.Hosting;

namespace Stl.Fusion.Client
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionWebSocketClient(this IServiceCollection services, 
            WebSocketClient.Options options)
        {
            services.TryAddSingleton(options);
            if (!services.HasService<WebSocketClient>())
                services.AddAsyncProcessSingleton<WebSocketClient>();
            return services;
        }

        public static IServiceCollection AddFusionWebSocketClient(this IServiceCollection services, 
            Action<WebSocketClient.Options>? optionsBuilder = null)
        {
            var options = new WebSocketClient.Options();
            optionsBuilder?.Invoke(options);
            return services.AddFusionWebSocketClient(options);
        }        
    }
}
