using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestEase;
using Stl.Extensibility;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Client.RestEase;
using Stl.Fusion.Internal;
using Stl.Reflection;
using Stl.Serialization;

namespace Stl.Fusion.Client
{
    public static class ServiceCollectionEx
    {
        // Common client-side services

        public static IServiceCollection AddFusionRestEaseServices(this IServiceCollection services)
        {
            services.TryAddTransient<ResponseDeserializer>(c => new JsonResponseDeserializer() {
                JsonSerializerSettings = JsonNetSerializer.DefaultSettings
            });
            services.TryAddTransient<ReplicaResponseDeserializer>();
            return services;
        }

        public static IServiceCollection AddFusionWebSocketClient(
            this IServiceCollection services, 
            WebSocketChannelProvider.Options options, 
            bool addTransient = false)
        {
            services.TryAddSingleton(options);
            services.TryAddSingleton<IChannelProvider, WebSocketChannelProvider>();
            return services.AddFusionClientCore().AddFusionRestEaseServices();
        }

        public static IServiceCollection AddFusionWebSocketClient(
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
            return services.AddFusionClientCore().AddFusionRestEaseServices();
        }

        // User-defined client-side services
        
        public static IServiceCollection AddRestEaseService<TInterface>(
            this IServiceCollection services)
            => services.AddRestEaseService(typeof(TInterface));
        public static IServiceCollection AddRestEaseService(
            this IServiceCollection services, 
            Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentOutOfRangeException(nameof(interfaceType));
            services.TryAddSingleton(interfaceType, c => {
                var httpClient = c.GetRequiredSpecialService<HttpClient>(interfaceType);
                var restClient = new RestClient(httpClient) {
                    ResponseDeserializer = c.GetRequiredService<ReplicaResponseDeserializer>()
                }.For(interfaceType);
                return restClient;
            });
            return services;
        }

        public static IServiceCollection AddReplicaService<TInterface>(
            this IServiceCollection services)
            => services.AddReplicaService(typeof(TInterface));
        public static IServiceCollection AddReplicaService(
            this IServiceCollection services, Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentOutOfRangeException(nameof(interfaceType));
            if (!typeof(IReplicaService).IsAssignableFrom(interfaceType))
                throw Errors.MustImplement<IReplicaService>(interfaceType);
            services.TryAddSingleton(interfaceType, c => {
                // 1. Create REST client for the service
                var httpClient = c.GetRequiredSpecialService<HttpClient>(interfaceType);
                var restClient = new RestClient(httpClient) {
                    ResponseDeserializer = c.GetRequiredService<ReplicaResponseDeserializer>()
                }.For(interfaceType);

                // 2. Create Replica Service 
                var proxyGenerator = c.GetRequiredService<IReplicaServiceProxyGenerator>();
                var proxyType = proxyGenerator.GetProxyType(interfaceType);
                var interceptors = c.GetRequiredService<ReplicaServiceInterceptor[]>();
                return proxyType.CreateInstance(interceptors, restClient);
            });
            return services;
        }
    }
}
