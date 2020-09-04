using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Client.RestEase;
using Stl.Fusion.Client.RestEase.Internal;
using Stl.Fusion.Internal;
using Stl.Reflection;
using Stl.Serialization;

namespace Stl.Fusion.Client
{
    public static class ServiceCollectionEx
    {
        // Common client-side services

        public static IServiceCollection AddFusionWebSocketClient(
            this IServiceCollection services,
            WebSocketChannelProvider.Options options)
            => services
                .AddRestEaseCore()
                .AddFusionWebSocketClientCore(options);

        public static IServiceCollection AddFusionWebSocketClient(
            this IServiceCollection services,
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null)
            => services
                .AddRestEaseCore()
                .AddFusionWebSocketClientCore(optionsBuilder);

        public static IServiceCollection AddRestEaseCore(this IServiceCollection services)
        {
            // FusionHttpMessageHandler (handles Fusion headers)
            services.AddHttpClient();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IHttpMessageHandlerBuilderFilter,
                FusionHttpMessageHandlerBuilderFilter>());
            services.TryAddTransient<FusionHttpMessageHandler>();

            // InterfaceCastProxyGenerator (used by ReplicaServices)
            services.TryAddSingleton<InterfaceCastInterceptor>();
            services.TryAddSingleton(c => InterfaceCastProxyGenerator.Default);
            services.TryAddSingleton(c => new [] { c.GetRequiredService<InterfaceCastInterceptor>() });

            // ResponseDeserializer & ReplicaResponseDeserializer
            services.TryAddTransient<ResponseDeserializer>(c => new JsonResponseDeserializer() {
                JsonSerializerSettings = JsonNetSerializer.DefaultSettings
            });
            services.TryAddTransient<RequestBodySerializer>(c => new JsonRequestBodySerializer() {
                JsonSerializerSettings = JsonNetSerializer.DefaultSettings
            });
            return services;
        }

        public static IServiceCollection AddFusionWebSocketClientCore(
            this IServiceCollection services,
            WebSocketChannelProvider.Options options)
        {
            services.TryAddSingleton(options);
            services.TryAddSingleton<IChannelProvider, WebSocketChannelProvider>();
            return services.AddFusionClientCore();
        }

        public static IServiceCollection AddFusionWebSocketClientCore(
            this IServiceCollection services,
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null)
        {
            services.TryAddSingleton(c => {
                var options = new WebSocketChannelProvider.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            services.TryAddSingleton<IChannelProvider, WebSocketChannelProvider>();
            return services.AddFusionClientCore().AddRestEaseCore();
        }

        // User-defined client-side services

        public static IServiceCollection AddRestEaseService<TService>(
            this IServiceCollection services,
            string? clientName = null)
            => services.AddRestEaseService(typeof(TService), clientName);
        public static IServiceCollection AddRestEaseService(
            this IServiceCollection services,
            Type clientType,
            string? clientName = null)
        {
            if (!(clientType.IsInterface && clientType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(clientType, true, nameof(clientType));
            clientName ??= clientType.FullName;

            services.TryAddSingleton(clientType, c => {
                var httpClientFactory = c.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(clientName);
                var restClient = new RestClient(httpClient) {
                    RequestBodySerializer = c.GetRequiredService<RequestBodySerializer>(),
                    ResponseDeserializer = c.GetRequiredService<ResponseDeserializer>(),
                };
                return restClient.For(clientType);
            });
            return services;
        }

        public static IServiceCollection AddRestEaseReplicaService<TClient>(
            this IServiceCollection services,
            string? clientName = null)
            where TClient : IRestEaseReplicaClient
            => services.AddRestEaseReplicaService(typeof(TClient), clientName);
        public static IServiceCollection AddRestEaseReplicaService<TService, TClient>(
            this IServiceCollection services,
            string? clientName = null)
            where TClient : IRestEaseReplicaClient
            => services.AddRestEaseReplicaService(typeof(TService), typeof(TClient), clientName);
        public static IServiceCollection AddRestEaseReplicaService(
            this IServiceCollection services,
            Type clientType,
            string? clientName = null)
            => services.AddRestEaseReplicaService(clientType, clientType, clientName);
        public static IServiceCollection AddRestEaseReplicaService(
            this IServiceCollection services,
            Type serviceType,
            Type clientType,
            string? clientName = null)
        {
            if (!(serviceType.IsInterface && serviceType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(serviceType, true, nameof(serviceType));
            if (!(clientType.IsInterface && clientType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(clientType, true, nameof(clientType));
            if (!typeof(IRestEaseReplicaClient).IsAssignableFrom(clientType))
                throw Errors.MustImplement<IRestEaseReplicaClient>(clientType, nameof(clientType));
            clientName ??= clientType.FullName;

            object Factory(IServiceProvider c)
            {
                // 1. Validate type
                var interceptor = c.GetRequiredService<ReplicaClientInterceptor>();
                interceptor.ValidateType(clientType);

                // 2. Create REST client (of clientType)
                var httpClientFactory = c.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(clientName);
                var client = new RestClient(httpClient) {
                    RequestBodySerializer = c.GetRequiredService<RequestBodySerializer>(),
                    ResponseDeserializer = c.GetRequiredService<ResponseDeserializer>()
                }.For(clientType);

                // 3. Create proxy mapping client to serviceType
                if (clientType != serviceType) {
                    var serviceProxyGenerator = c.GetRequiredService<IInterfaceCastProxyGenerator>();
                    var serviceProxyType = serviceProxyGenerator.GetProxyType(serviceType);
                    var serviceInterceptors = c.GetRequiredService<InterfaceCastInterceptor[]>();
                    client = serviceProxyType.CreateInstance(serviceInterceptors, client);
                }

                // 4. Create Replica Client
                var replicaProxyGenerator = c.GetRequiredService<IReplicaClientProxyGenerator>();
                var replicaProxyType = replicaProxyGenerator.GetProxyType(serviceType);
                var replicaInterceptors = c.GetRequiredService<ReplicaClientInterceptor[]>();
                client = replicaProxyType.CreateInstance(replicaInterceptors, client);
                return client;
            }

            services.TryAddSingleton(serviceType, Factory);
            return services;
        }
    }
}
