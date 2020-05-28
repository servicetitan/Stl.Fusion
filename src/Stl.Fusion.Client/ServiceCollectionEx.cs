using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client.RestEase;
using Stl.Serialization;

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
        
        public static IServiceCollection AddFusionRestEaseServices(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(SafeJsonNetSerializer<>));
            services.TryAddTransient<ResponseDeserializer>(c => new JsonResponseDeserializer() {
                JsonSerializerSettings = new JsonSerializerSettings() {
                    TypeNameHandling = TypeNameHandling.None,
                }
            });
            services.TryAddTransient<ReplicaResponseDeserializer>();
            return services;
        }
    }
}
