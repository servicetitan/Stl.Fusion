using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.CommandR;
using Stl.Fusion.Interception;
using Stl.Internal;
using Stl.Time;

namespace Stl.Fusion
{
    public readonly struct FusionBuilder
    {
        private class AddedTag { }
        private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());
        private static readonly HashSet<Type> GenericStateInterfaces = new() {
            typeof(IState<>),
            typeof(IMutableState<>),
            typeof(IComputedState<>),
            typeof(ILiveState<>),
        };

        public IServiceCollection Services { get; }

        internal FusionBuilder(IServiceCollection services)
        {
            Services = services;
            if (Services.Contains(AddedTagDescriptor))
                return;
            // We want above Contains call to run in O(1), so...
            Services.Insert(0, AddedTagDescriptor);
            Services.AddCommander();

            // Common services
            Services.AddOptions();
            Services.TryAddSingleton(SystemClock.Instance);
            Services.AddCommander().AddInvalidationHandler();
            // Compute services & their dependencies
            Services.TryAddSingleton(_ => ComputeServiceProxyGenerator.Default);
            Services.TryAddSingleton<IComputedOptionsProvider, ComputedOptionsProvider>();
            Services.TryAddSingleton(new ArgumentHandlerProvider.Options());
            Services.TryAddSingleton<IArgumentHandlerProvider, ArgumentHandlerProvider>();
            Services.TryAddSingleton(new ComputeMethodInterceptor.Options());
            Services.TryAddSingleton<ComputeMethodInterceptor>();
            Services.TryAddSingleton(new ComputeServiceInterceptor.Options());
            Services.TryAddSingleton<ComputeServiceInterceptor>();
            Services.TryAddSingleton(c => new [] { c.GetRequiredService<ComputeServiceInterceptor>() });
            Services.TryAddSingleton<IErrorRewriter, ErrorRewriter>();
            // States & their dependencies
            Services.TryAddTransient<IStateFactory, StateFactory>();
            Services.TryAddTransient(typeof(IMutableState<>), typeof(MutableState<>));
            Services.TryAddSingleton(new UpdateDelayer.Options());
            Services.TryAddTransient<IUpdateDelayer, UpdateDelayer>();
        }

        static FusionBuilder()
        {
            var nonReplicableAttributeTypes = new HashSet<Type>() {
                typeof(ServiceAttributeBase),
                typeof(AsyncStateMachineAttribute),
                typeof(ComputeMethodAttribute),
            };
            foreach (var type in nonReplicableAttributeTypes)
                if (!AttributesToAvoidReplicating.Contains(type))
                    AttributesToAvoidReplicating.Add(type);
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
            Services.TryAddSingleton(_ => ReplicaServiceProxyGenerator.Default);
            Services.TryAddSingleton(new ReplicaMethodInterceptor.Options());
            Services.TryAddSingleton<ReplicaMethodInterceptor>();
            Services.TryAddSingleton(new ReplicaServiceInterceptor.Options());
            Services.TryAddSingleton<ReplicaServiceInterceptor>();
            Services.TryAddSingleton(c => new [] { c.GetRequiredService<ReplicaServiceInterceptor>() });
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
        public FusionBuilder AddComputeService<TService, TImplementation>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImplementation : class, TService
            => AddComputeService(typeof(TService), typeof(TImplementation), lifetime);

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
            Services.AddCommander().AddCommandService(serviceType, implementationType);
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

            // Try register Options type based for .ctor(Options options, ...)
            foreach (var ctor in implementationType.GetConstructors()) {
                if (!ctor.IsPublic)
                    continue;
                var pOptions = ctor.GetParameters().FirstOrDefault();
                if (pOptions == null)
                    continue; // Must be the first .ctor parameter
                if (pOptions.Name != "options")
                    continue; // Must be named "options"
                var tOptions = pOptions.ParameterType;
                if (tOptions.GetConstructor(Array.Empty<Type>()) == null)
                    continue; // Must have new() constructor
                Services.TryAddTransient(tOptions);
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
