using System;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestEase;
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

        public static IServiceCollection AddRestEaseClient<TClient>(
            this IServiceCollection services,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
            => services.AddRestEaseClient(typeof(TClient), baseAddress, httpClientResolver);
        public static IServiceCollection AddRestEaseClient(
            this IServiceCollection services,
            Type clientType,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
        {
            if (!(clientType.IsInterface && clientType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(clientType, true, nameof(clientType));

            httpClientResolver ??= DefaultHttpClientResolver(baseAddress);
            services.TryAddSingleton(clientType, c => {
                var httpClient = httpClientResolver.Invoke(c);
                var restClient = new RestClient(httpClient) {
                    ResponseDeserializer = c.GetRequiredService<ReplicaResponseDeserializer>()
                }.For(clientType);
                return restClient;
            });
            return services;
        }

        public static IServiceCollection AddRestEaseReplicaService<TClient>(
            this IServiceCollection services,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
            where TClient : IRestEaseReplicaClient
            => services.AddRestEaseReplicaService(typeof(TClient), baseAddress, httpClientResolver);
        public static IServiceCollection AddRestEaseReplicaService<TService, TClient>(
            this IServiceCollection services,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
            where TClient : IRestEaseReplicaClient
            => services.AddRestEaseReplicaService(typeof(TService), typeof(TClient), baseAddress, httpClientResolver);
        public static IServiceCollection AddRestEaseReplicaService(
            this IServiceCollection services,
            Type clientType,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
            => services.AddRestEaseReplicaService(clientType, clientType, baseAddress, httpClientResolver);
        public static IServiceCollection AddRestEaseReplicaService(
            this IServiceCollection services,
            Type serviceType,
            Type clientType,
            string? baseAddress = null,
            Func<IServiceProvider, HttpClient>? httpClientResolver = null)
        {
            if (!(serviceType.IsInterface && serviceType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(serviceType, true, nameof(serviceType));
            if (!(clientType.IsInterface && clientType.IsPublic))
                throw Internal.Errors.InterfaceTypeExpected(clientType, true, nameof(clientType));
            if (!typeof(IRestEaseReplicaClient).IsAssignableFrom(clientType))
                throw Errors.MustImplement<IRestEaseReplicaClient>(clientType, nameof(clientType));

            httpClientResolver ??= DefaultHttpClientResolver(baseAddress);

            object Factory(IServiceProvider c)
            {
                // 1. Validate type
                var interceptor = c.GetRequiredService<ReplicaClientInterceptor>();
                interceptor.ValidateType(clientType);

                // 2. Create REST client for the service
                var httpClient = httpClientResolver.Invoke(c);
                var restClient = new RestClient(httpClient) {
                    ResponseDeserializer = c.GetRequiredService<ReplicaResponseDeserializer>()
                }.For(clientType);

                // 3. Create Replica Client
                var clientProxyGenerator = c.GetRequiredService<IReplicaClientProxyGenerator>();
                var clientProxyType = clientProxyGenerator.GetProxyType(clientType);
                var clientInterceptors = c.GetRequiredService<ReplicaClientInterceptor[]>();
                var client = clientProxyType.CreateInstance(clientInterceptors, restClient);
                if (clientType == serviceType)
                    return client;

                // 4. Create Replica Service
                var serviceProxyGenerator = c.GetRequiredService<IInterfaceCastProxyGenerator>();
                var serviceProxyType = serviceProxyGenerator.GetProxyType(serviceType);
                var serviceInterceptors = c.GetRequiredService<InterfaceCastInterceptor[]>();
                var service = serviceProxyType.CreateInstance(serviceInterceptors, client);
                return service;
            }

            var isScoped = typeof(IScopedComputedService).IsAssignableFrom(clientType);
            if (isScoped)
                services.TryAddScoped(serviceType, Factory);
            else
                services.TryAddSingleton(serviceType, Factory);
            return services;
        }

        private static Func<IServiceProvider, HttpClient> DefaultHttpClientResolver(string? baseAddress = null)
            => services => {
                var httpClient = services.GetRequiredService<HttpClient>();
                TrySetBaseAddress(httpClient, baseAddress);
                return httpClient;
            };

        private static void TrySetBaseAddress(HttpClient httpClient, string? baseAddress)
        {
            if (baseAddress == null)
                return;
            if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var baseUri))
                if (!Uri.TryCreate(httpClient.BaseAddress, baseAddress, out baseUri))
                    throw Internal.Errors.InvalidUri(baseAddress);
            httpClient.BaseAddress = baseUri;
        }
    }
}
