using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Reflection;

namespace Stl.Fusion
{
    public static class ServiceCollectionEx
    {
        private static readonly HashSet<Type> GenericStateInterfaces = new HashSet<Type>() {
            typeof(IState<>),
            typeof(IMutableState<>),
            typeof(IComputedState<>),
            typeof(ILiveState<>),
            typeof(ILiveState<,>),
        };

        public static IServiceCollection AddFusionCore(this IServiceCollection services)
        {
            // ComputeServiceProxyGenerator
            services.TryAddSingleton(new ArgumentHandlerProvider.Options());
            services.TryAddSingleton<IArgumentHandlerProvider, ArgumentHandlerProvider>();
            services.TryAddSingleton(new ComputeServiceInterceptor.Options());
            services.TryAddSingleton<ComputeServiceInterceptor>();
            services.TryAddSingleton(c => ComputeServiceProxyGenerator.Default);
            services.TryAddSingleton(c => new [] { c.GetRequiredService<ComputeServiceInterceptor>() });
            // StateFactory
            services.AddScoped<IStateFactory, StateFactory>();
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
            services.TryAddSingleton(new ReplicaClientInterceptor.Options());
            services.TryAddSingleton<ReplicaClientInterceptor>();
            services.TryAddSingleton(c => ReplicaClientProxyGenerator.Default);
            services.TryAddSingleton(c => new [] { c.GetRequiredService<ReplicaClientInterceptor>() });
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

        // AddComputeService

        public static IServiceCollection AddComputeService<TService>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            => services.AddComputeService(typeof(TService), lifetime);
        public static IServiceCollection AddComputeService<TService, TImpl>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImpl : class, TService
            => services.AddComputeService(typeof(TService), typeof(TImpl), lifetime);

        public static IServiceCollection AddComputeService(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            => services.AddComputeService(serviceType, serviceType, lifetime);
        public static IServiceCollection AddComputeService(
            this IServiceCollection services,
            Type serviceType, Type implementationType,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));

            object Factory(IServiceProvider c)
            {
                // We should try to validate it here because if the type doesn't
                // have any virtual methods (which might be a mistake), no calls
                // will be intercepted, so no error will be thrown later.
                var interceptor = c.GetRequiredService<ComputeServiceInterceptor>();
                interceptor.ValidateType(implementationType);
                var proxyGenerator = c.GetRequiredService<IComputeServiceProxyGenerator>();
                var proxyType = proxyGenerator.GetProxyType(implementationType);
                return c.Activate(proxyType);
            }

            var descriptor = new ServiceDescriptor(
                serviceType ?? implementationType, Factory, lifetime);
            services.TryAdd(descriptor);
            return services;
        }

        // AddState

        public static IServiceCollection AddState(
            this IServiceCollection services, Type implementationType,
            Func<IServiceProvider, IState>? factory = null)
        {
            if (implementationType.IsValueType)
                throw new ArgumentOutOfRangeException(nameof(implementationType));
            var isRegistered = false;

            var tInterfaces = new List<Type>();
            if (implementationType.IsInterface) {
                tInterfaces.Add(implementationType);
                if (factory == null)
                    throw new ArgumentNullException(nameof(factory));
            }
            tInterfaces.AddRange(implementationType.GetInterfaces());

            foreach (var tInterface in tInterfaces) {
                if (!tInterface.IsConstructedGenericType)
                    continue;
                var gInterface = tInterface.GetGenericTypeDefinition();
                if (GenericStateInterfaces.Contains(gInterface)) {
                    if (factory != null)
                        services.TryAddTransient(tInterface, factory);
                    else
                        services.TryAddTransient(tInterface, implementationType);
                    isRegistered = true;
                }
            }
            if (!isRegistered)
                throw Errors.MustImplement(implementationType, typeof(IState<>), nameof(implementationType));

            if (!implementationType.IsInterface) {
                if (factory != null)
                    services.TryAddTransient(implementationType, factory);
                else
                    services.TryAddTransient(implementationType);
            }

            // Registering IOption types based on .ctor parameters
            foreach (var ctor in implementationType.GetConstructors()) {
                if (!ctor.IsPublic)
                    continue;
                var parameters = ctor.GetParameters();
                if (parameters.Length < 1)
                    continue;
                var optionsType = parameters[0].ParameterType;
                if (!typeof(IOptions).IsAssignableFrom(optionsType))
                    continue;
                services.TryAddTransient(optionsType);
            }
            return services;
        }

        public static IServiceCollection AddState<TImplementation>(
            this IServiceCollection services)
            where TImplementation : class, IState
            => services.AddState(typeof(TImplementation));

        public static IServiceCollection AddState<TImplementation>(
            this IServiceCollection services, Func<IServiceProvider, TImplementation> factory)
            where TImplementation : class, IState
            => services.AddState(typeof(TImplementation), factory);
    }
}
