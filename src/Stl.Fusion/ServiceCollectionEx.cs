using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Extensibility;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Fusion.UI;

namespace Stl.Fusion
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusionCore(this IServiceCollection services)
        {
            // Registry
            services.TryAddSingleton(ComputedRegistry.Default);
            // ComputedServiceProxyGenerator
            services.TryAddSingleton(new ComputedServiceInterceptor.Options());
            services.TryAddSingleton<ComputedServiceInterceptor>();
            services.TryAddSingleton(c => ComputedServiceProxyGenerator.Default);
            services.TryAddSingleton(c => new [] { c.GetRequiredService<ComputedServiceInterceptor>() });
            return services;
        }

        public static IServiceCollection AddFusionServerCore(this IServiceCollection services)
        {
            // Publisher
            services.TryAddSingleton(new Publisher.Options());
            services.TryAddSingleton<IPublisher, Publisher>();
            return services.AddFusionCore();
        }

        public static IServiceCollection AddFusionClientCore(this IServiceCollection services)
        {
            // ReplicaServiceProxyGenerator
            services.TryAddSingleton(new ReplicaServiceInterceptor.Options());
            services.TryAddSingleton<ReplicaServiceInterceptor>();
            services.TryAddSingleton(c => ReplicaServiceProxyGenerator.Default);
            services.TryAddSingleton(c => new [] { c.GetRequiredService<ReplicaServiceInterceptor>() });
            // Replicator
            services.TryAddSingleton(new Replicator.Options());
            services.TryAddSingleton<IReplicator, Replicator>();
            // Client-side services
            services.TryAddSingleton(new UpdateDelayer.Options());
            services.TryAddSingleton<IUpdateDelayer, UpdateDelayer>();
            return services.AddFusionCore();
        }

        public static IServiceCollection AddFusion(this IServiceCollection services, 
            bool addServerCore, bool addClientCore)
        {
            if (addServerCore)
                services.AddFusionServerCore();
            if (addClientCore)
                services.AddFusionClientCore();
            return services.AddFusionCore();
        }

        // AddComputedService

        public static IServiceCollection AddComputedService<TService>(this IServiceCollection services)
            where TService : class
            => services.AddComputedService(typeof(TService));
        public static IServiceCollection AddComputedService<TService, TImpl>(this IServiceCollection services)
            where TService : class
            where TImpl : class, TService
            => services.AddComputedService(typeof(TService), typeof(TImpl));
        
        public static IServiceCollection AddComputedService(this IServiceCollection services, Type type)
            => services.AddComputedService(type, type);
        public static IServiceCollection AddComputedService(this IServiceCollection services, 
            Type type, Type implementationType)
        {
            if (!type.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));
            if (!typeof(IComputedService).IsAssignableFrom(implementationType))
                throw Errors.MustImplement<IComputedService>(implementationType);
            services.TryAddSingleton(type, c => {
                // We should try to validate it here because if the type doesn't
                // have any virtual methods (which might be a mistake), no calls
                // will be intercepted, so no error will be thrown later.
                var interceptor = c.GetRequiredService<ComputedServiceInterceptor>();
                interceptor.ValidateType(implementationType);  

                var proxyGenerator = c.GetRequiredService<IComputedServiceProxyGenerator>();
                var proxyType = proxyGenerator.GetProxyType(implementationType);
                return c.Activate(proxyType);
            });
            return services;
        }
    }
}
