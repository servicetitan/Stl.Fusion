using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Channels;
using Stl.Extensibility;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Interception;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddFusion(this IServiceCollection services)
        {
            services.TryAddSingleton(c => ComputedProxyGenerator.Default);
            services.TryAddSingleton<ComputedInterceptor>();
            services.TryAddSingleton(c => ComputedRegistry.Default);
            services.TryAddSingleton(c => ArgumentComparerProvider.Default);
            services.TryAddSingleton(c => ComputeRetryPolicy.Default);
            services.TryAddSingleton(c => ComputedReplicaRetryPolicy.Default);
            services.TryAddComputedProvider<CustomFunction>();
            return services;
        }

        public static IServiceCollection AddFusionPublisher(this IServiceCollection services,
            Symbol publisherId, IGenerator<Symbol>? publicationIdGenerator = null)
        {
            if (services.HasService<IPublisher>())
                return services;

            IGenerator<Symbol> PublicationIdGeneratorResolver(IServiceProvider c) 
                => publicationIdGenerator ?? c.GetRequiredService<IGenerator<Symbol>>();

            var channelHub = new ChannelHub<PublicationMessage>();
            return services.AddSingleton<IPublisher>(
                c => new Publisher(publisherId, channelHub, PublicationIdGeneratorResolver(c))); 
        }

        public static IServiceCollection AddFusionReplicator(this IServiceCollection services,
            Func<Channel<PublicationMessage>, Symbol> publisherIdProvider)
        {
            if (services.HasService<IReplicator>())
                return services;

            var channelHub = new ChannelHub<PublicationMessage>();
            return services.AddSingleton<IReplicator>(
                c => new Replicator(channelHub, publisherIdProvider)); 
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
