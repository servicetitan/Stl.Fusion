using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Client
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionWebSocketChannelProvider(
            this IServiceCollection services, 
            WebSocketChannelProvider.Options options, 
            bool addTransient = false)
        {
            services.TryAddSingleton(options);
            services.TryAddSingleton<IChannelProvider, WebSocketChannelProvider>();
            return services;
        }

        public static IServiceCollection AddFusionWebSocketChannelProvider(
            this IServiceCollection services, 
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null, 
            bool addTransient = false)
        {
            services.TryAddSingleton(c => {
                var options = new WebSocketChannelProvider.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddSingleton<IChannelProvider, WebSocketChannelProvider>();
            return services;
        }        
    }
}
