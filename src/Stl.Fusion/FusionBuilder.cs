using Castle.DynamicProxy.Generators;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion;
using Stl.Extensibility;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Multitenancy;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Fusion.UI;
using Stl.Multitenancy;
using Stl.Versioning.Providers;

namespace Stl.Fusion;

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
        Services.TryAddSingleton(new ComputeServiceProxyGenerator.Options());
        Services.TryAddSingleton<IComputeServiceProxyGenerator, ComputeServiceProxyGenerator>();
        Services.TryAddSingleton<IComputedOptionsProvider, ComputedOptionsProvider>();
        Services.TryAddSingleton<IMatchingTypeFinder>(_ => new MatchingTypeFinder());
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
        
        // Multitenancy
        services.TryAddSingleton<ITenantRegistry<Unit>, SingleTenantRegistry<Unit>>();
        services.TryAddTransient<ITenantRegistry>(c => c.GetRequiredService<ITenantRegistry<Unit>>());
        services.TryAddSingleton<DefaultTenantResolver<Unit>.Options>();
        services.TryAddSingleton<ITenantResolver<Unit>, DefaultTenantResolver<Unit>>();
        services.TryAddTransient<ITenantResolver>(c => c.GetRequiredService<ITenantResolver<Unit>>());
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

    public FusionBuilder AddPublisher(
        Func<IServiceProvider, PublisherOptions>? optionsFactory = null)
    {
        // Publisher
        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<IPublisher, Publisher>();
        return this;
    }

    public FusionBuilder AddReplicator(
        Func<IServiceProvider, ReplicatorOptions>? optionsFactory = null)
    {
        // ReplicaServiceProxyGenerator
        Services.TryAddSingleton(new ReplicaServiceProxyGenerator.Options());
        Services.TryAddSingleton<IReplicaServiceProxyGenerator, ReplicaServiceProxyGenerator>();
        Services.TryAddSingleton(new ReplicaMethodInterceptor.Options());
        Services.TryAddSingleton<ReplicaMethodInterceptor>();
        Services.TryAddSingleton(new ReplicaServiceInterceptor.Options());
        Services.TryAddSingleton<ReplicaServiceInterceptor>();
        Services.TryAddSingleton(c => new [] { c.GetRequiredService<ReplicaServiceInterceptor>() });
        // Replicator
        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
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
        configureFusionAuthentication(fusionAuth);
        return this;
    }

    // AddOperationReprocessor

    public FusionBuilder AddOperationReprocessor(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        => AddOperationReprocessor<OperationReprocessor>(optionsFactory);

    public FusionBuilder AddOperationReprocessor<TOperationReprocessor>(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        where TOperationReprocessor : class, IOperationReprocessor
    {
        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        if (!Services.HasService<IOperationReprocessor>()) {
            Services.AddTransient<TOperationReprocessor>();
            Services.AddTransient<IOperationReprocessor>(c => c.GetRequiredService<TOperationReprocessor>());
            Services.AddCommander().AddHandlers<TOperationReprocessor>();
        }
        return this;
    }
}
