using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Internal;
using Stl.Time;

namespace Stl.Fusion
{
    public readonly struct FusionBuilder
    {
        private static readonly Box<bool> IsInitialized = Box.New(false);

        private class AddedTag { }
        private static readonly ServiceDescriptor AddedTagDescriptor =
            new ServiceDescriptor(typeof(AddedTag), new AddedTag());
        private static readonly HashSet<Type> GenericStateInterfaces = new HashSet<Type>() {
            typeof(IState<>),
            typeof(IMutableState<>),
            typeof(IComputedState<>),
            typeof(ILiveState<>),
        };

        public IServiceCollection Services { get; }

        internal FusionBuilder(IServiceCollection services)
        {
            Initialize();
            Services = services;
            if (Services.Contains(AddedTagDescriptor))
                return;
            // We want above Contains call to run in O(1), so...
            Services.Insert(0, AddedTagDescriptor);

            // Common services
            Services.AddOptions();
            Services.TryAddSingleton(SystemClock.Instance);
            // Compute services & their dependencies
            Services.TryAddSingleton<IComputedOptionsProvider, ComputedOptionsProvider>();
            Services.TryAddSingleton(new ArgumentHandlerProvider.Options());
            Services.TryAddSingleton<IArgumentHandlerProvider, ArgumentHandlerProvider>();
            Services.TryAddSingleton(new ComputeServiceInterceptor.Options());
            Services.TryAddSingleton<ComputeServiceInterceptor>();
            Services.TryAddSingleton(c => ComputeServiceProxyGenerator.Default);
            Services.TryAddSingleton(c => new [] { c.GetRequiredService<ComputeServiceInterceptor>() });
            Services.TryAddSingleton<IErrorRewriter, ErrorRewriter>();
            // States & their dependencies
            Services.TryAddTransient<IStateFactory, StateFactory>();
            Services.TryAddTransient(typeof(IMutableState<>), typeof(MutableState<>));
            Services.TryAddSingleton(new UpdateDelayer.Options());
            Services.TryAddTransient<IUpdateDelayer, UpdateDelayer>();
        }

        public static void Initialize(HashSet<Type>? nonReplicableAttributes = null)
        {
            if (IsInitialized.Value) return;
            lock (IsInitialized) {
                if (IsInitialized.Value) return;
                IsInitialized.Value = true;
                // Castle.DynamicProxy fails while trying to replicate
                // these attributes in WASM in .NET 5.0
                nonReplicableAttributes ??= new HashSet<Type>() {
                    typeof(AsyncStateMachineAttribute),
                    typeof(ComputeMethodAttribute),
                };
                foreach (var type in nonReplicableAttributes)
                    Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(type);
            }
        }

        // AddPublisher, AddReplicator

        public FusionBuilder AddPublisher(Action<IServiceProvider, Publisher.Options>? configurePublisherOptions = null)
        {
            // Publisher
            Services.TryAddSingleton(c => {
                var options = new Publisher.Options();
                configurePublisherOptions?.Invoke(c, options);
                return options;
            });
            Services.TryAddSingleton<IPublisher, Publisher>();
            return this;
        }

        public FusionBuilder AddReplicator(Action<IServiceProvider, Replicator.Options>? configureReplicatorOptions = null)
        {
            // ReplicaServiceProxyGenerator
            Services.TryAddSingleton(new ReplicaClientInterceptor.Options());
            Services.TryAddSingleton<ReplicaClientInterceptor>();
            Services.TryAddSingleton(c => ReplicaClientProxyGenerator.Default);
            Services.TryAddSingleton(c => new [] { c.GetRequiredService<ReplicaClientInterceptor>() });
            // Replicator
            Services.TryAddSingleton(c => {
                var options = new Replicator.Options();
                configureReplicatorOptions?.Invoke(c, options);
                return options;
            });
            Services.TryAddSingleton<IReplicator, Replicator>();
            return this;
        }

        // AddComputeService

        public FusionBuilder AddComputeService<TService>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            => AddComputeService(typeof(TService), lifetime);
        public FusionBuilder AddComputeService<TService, TImpl>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImpl : class, TService
            => AddComputeService(typeof(TService), typeof(TImpl), lifetime);

        public FusionBuilder AddComputeService(
            Type serviceType,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            => AddComputeService(serviceType, serviceType, lifetime);
        public FusionBuilder AddComputeService(
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

            var descriptor = new ServiceDescriptor(serviceType, Factory, lifetime);
            Services.TryAdd(descriptor);
            return this;
        }

        // AddState

        public FusionBuilder AddState(
            Type implementationType,
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
                        Services.TryAddTransient(tInterface, factory);
                    else
                        Services.TryAddTransient(tInterface, implementationType);
                    isRegistered = true;
                }
            }
            if (!isRegistered)
                throw Errors.MustImplement(implementationType, typeof(IState<>), nameof(implementationType));

            if (!implementationType.IsInterface) {
                if (factory != null)
                    Services.TryAddTransient(implementationType, factory);
                else
                    Services.TryAddTransient(implementationType);
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
                Services.TryAddTransient(optionsType);
            }
            return this;
        }

        public FusionBuilder AddState<TImplementation>()
            where TImplementation : class, IState
            => AddState(typeof(TImplementation));

        public FusionBuilder AddState<TImplementation>(
            Func<IServiceProvider, TImplementation> factory)
            where TImplementation : class, IState
            => AddState(typeof(TImplementation), factory);
    }
}
