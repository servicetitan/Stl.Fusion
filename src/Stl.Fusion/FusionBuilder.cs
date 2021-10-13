using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.Conversion;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Operations;
using Stl.Fusion.Interception;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Fusion.UI;
using Stl.Time;
using Stl.Versioning;
using Stl.Versioning.Providers;

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
            Services.AddConverters();
            Services.TryAddSingleton(MomentClockSet.Default);
            Services.TryAddSingleton(c => c.GetRequiredService<MomentClockSet>().SystemClock);
            Services.TryAddSingleton(LTagVersionGenerator.Default);
            Services.TryAddSingleton(ClockBasedVersionGenerator.DefaultCoarse);

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
            Services.TryAddScoped<IUICommandTracker, UICommandTracker>();
            Services.TryAddTransient<IUpdateDelayer>(c => new UpdateDelayer(c.UICommandTracker()));

            // CommandR, command completion and invalidation
            var commander = Services.AddCommander();
            Services.TryAddSingleton<AgentInfo>();
            Services.TryAddSingleton<InvalidationInfoProvider>();

            // Transient operation scope & its provider
            Services.TryAddTransient<TransientOperationScope>();
            if (!Services.HasService<TransientOperationScopeProvider>()) {
                Services.AddSingleton<TransientOperationScopeProvider>();
                commander.AddHandlers<TransientOperationScopeProvider>();
            }

            // Nested command logger
            if (!Services.HasService<NestedCommandLogger>()) {
                Services.AddSingleton<NestedCommandLogger>();
                commander.AddHandlers<NestedCommandLogger>();
            }

            // Operation completion - notifier & producer
            Services.TryAddSingleton<OperationCompletionNotifier.Options>();
            Services.TryAddSingleton<IOperationCompletionNotifier, OperationCompletionNotifier>();
            Services.TryAddSingleton<CompletionProducer.Options>();
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(
                typeof(IOperationCompletionListener),
                typeof(CompletionProducer)));

            // Command completion handler performing invalidations
            Services.TryAddSingleton<InvalidateOnCompletionCommandHandler.Options>();
            if (!Services.HasService<InvalidateOnCompletionCommandHandler>()) {
                Services.AddSingleton<InvalidateOnCompletionCommandHandler>();
                commander.AddHandlers<InvalidateOnCompletionCommandHandler>();
            }

            // Catch-all completion handler
            if (!Services.HasService<CatchAllCompletionHandler>()) {
                Services.AddSingleton<CatchAllCompletionHandler>();
                commander.AddHandlers<CatchAllCompletionHandler>();
            }
        }

        static FusionBuilder()
        {
            var nonReplicableAttributeTypes = new HashSet<Type>() {
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
            if (Services.Any(d => d.ServiceType == serviceType))
                return this;

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
            Services.Add(descriptor);
            Services.AddCommander().AddHandlers(serviceType, implementationType);
            return this;
        }

        // AddAuthentication

        public FusionAuthenticationBuilder AddAuthentication()
            => new(this);
        public FusionBuilder AddAuthentication(Action<FusionAuthenticationBuilder> configureFusionAuthentication)
        {
            var fusionAuth = AddAuthentication();
            configureFusionAuthentication.Invoke(fusionAuth);
            return this;
        }

        // AddOperationReprocessor

        public FusionBuilder AddOperationReprocessor(
            Action<IServiceProvider, OperationReprocessor.Options>? optionsBuilder = null)
            => AddOperationReprocessor<OperationReprocessor>(optionsBuilder);

        public FusionBuilder AddOperationReprocessor<TOperationReprocessor>(
            Action<IServiceProvider, OperationReprocessor.Options>? optionsBuilder = null)
            where TOperationReprocessor : class, IOperationReprocessor
        {
            Services.TryAddSingleton(c => {
                var options = new OperationReprocessor.Options();
                optionsBuilder?.Invoke(c, options);
                return options;
            });
            if (!Services.HasService<IOperationReprocessor>()) {
                Services.AddTransient<TOperationReprocessor>();
                Services.AddTransient<IOperationReprocessor>(c => c.GetRequiredService<TOperationReprocessor>());
                Services.AddCommander().AddHandlers<TOperationReprocessor>();
            }
            return this;
        }
    }
}
