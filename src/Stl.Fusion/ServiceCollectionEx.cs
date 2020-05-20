using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Extensibility;
using Stl.Fusion.Bridge;
using Stl.Fusion.Interception;

namespace Stl.Fusion
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusion(this IServiceCollection services)
        {
            // Registry
            services.TryAddSingleton(ComputedRegistry.Default);

            // Call interception & related services
            services.TryAddSingleton(c => ComputedProxyGenerator.Default);
            services.TryAddSingleton(new ComputedInterceptor.Options());
            services.TryAddSingleton<ComputedInterceptor>();
            services.TryAddSingleton(c => new [] { c.GetRequiredService<ComputedInterceptor>() });

            // Everything else
            services.TryAddSingleton(new Publisher.Options());
            services.TryAddSingleton<IPublisher, Publisher>();
            services.TryAddSingleton(new Replicator.Options());
            services.TryAddSingleton<IReplicator, Replicator>();
            
            // Built-in computed providers
            services.TryAddComputedProvider<CustomFunction>();
            return services;
        }

        // TryAddComputedProvider & AddComputedProvider

        public static IServiceCollection TryAddComputedProvider<TService>(this IServiceCollection services)
            where TService : class
            => services.TryAddComputedProvider(typeof(TService));
        public static IServiceCollection TryAddComputedProvider<TService, TImpl>(this IServiceCollection services)
            where TService : class
            where TImpl : class, TService
            => services.TryAddComputedProvider(typeof(TService), typeof(TImpl));

        public static IServiceCollection TryAddComputedProvider(this IServiceCollection services, Type type) 
            => services.TryAddComputedProvider(type, type);
        public static IServiceCollection TryAddComputedProvider(this IServiceCollection services, Type type, Type implementationType) 
            => services.HasService(type) ? services : services.AddComputedProvider(type, implementationType);

        public static IServiceCollection AddComputedProvider<TService>(this IServiceCollection services)
            where TService : class
            => services.AddComputedProvider(typeof(TService));
        public static IServiceCollection AddComputedProvider<TService, TImpl>(this IServiceCollection services)
            where TService : class
            where TImpl : class, TService
            => services.AddComputedProvider(typeof(TService), typeof(TImpl));
        
        public static IServiceCollection AddComputedProvider(this IServiceCollection services, Type type)
            => services.AddComputedProvider(type, type);
        public static IServiceCollection AddComputedProvider(this IServiceCollection services, 
            Type type, Type implementationType)
        {
            if (!type.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));
            return services.AddSingleton(type, c => {
                var proxyGenerator = c.GetRequiredService<IComputedProxyGenerator>();
                var proxyType = proxyGenerator.GetProxyType(implementationType);
                return c.Activate(proxyType);
            });
        }
    }
}
