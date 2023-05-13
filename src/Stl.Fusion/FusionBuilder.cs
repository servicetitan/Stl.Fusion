using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Fusion.Multitenancy;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Fusion.UI;
using Stl.Interception;
using Stl.Multitenancy;
using Stl.Versioning.Providers;

namespace Stl.Fusion;

public readonly struct FusionBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }

    internal FusionBuilder(
        IServiceCollection services,
        Action<FusionBuilder>? configure)
    {
        Services = services;
        if (Services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);
        Services.AddCommander();

        // Common services
        Services.AddOptions();
        Services.AddConverters();
        Services.TryAddSingleton(_ => MomentClockSet.Default);
        Services.TryAddSingleton(c => c.GetRequiredService<MomentClockSet>().SystemClock);
        Services.TryAddSingleton(_ => LTagVersionGenerator.Default);
        Services.TryAddSingleton(_ => ClockBasedVersionGenerator.DefaultCoarse);

        // Compute services & their dependencies
        Services.TryAddSingleton(_ => new ComputedOptionsProvider());
        Services.TryAddSingleton(_ => TransientErrorDetector.DefaultPreferTransient.For<IComputed>());
        Services.TryAddSingleton(_ => new ComputeMethodInterceptor.Options());
        Services.TryAddSingleton(c => new ComputeMethodInterceptor(
            c.GetRequiredService<ComputeMethodInterceptor.Options>(), c));
        Services.TryAddSingleton(_ => new ComputeServiceInterceptor.Options());
        Services.TryAddSingleton(c => new ComputeServiceInterceptor(
            c.GetRequiredService<ComputeServiceInterceptor.Options>(), c));

        // States
        Services.TryAddSingleton(c => new MixedModeService<IStateFactory>.Singleton(new StateFactory(c), c));
        Services.TryAddScoped(c => new MixedModeService<IStateFactory>.Scoped(new StateFactory(c), c));
        Services.TryAddTransient(c => c.GetRequiredMixedModeService<IStateFactory>());
        Services.TryAddSingleton(typeof(MutableState<>.Options));
        Services.TryAddTransient(typeof(IMutableState<>), typeof(MutableState<>));

        // Update delayer & UI action tracker
        Services.TryAddSingleton(_ => new UIActionTracker.Options());
        Services.TryAddScoped<UIActionTracker>(c => new UIActionTracker(
            c.GetRequiredService<UIActionTracker.Options>(), c));
        Services.TryAddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker()));

        // CommandR, command completion and invalidation
        var commander = Services.AddCommander();
        Services.TryAddSingleton(_ => new AgentInfo());
        Services.TryAddSingleton(c => new InvalidationInfoProvider(
            c.Commander(), c.GetRequiredService<CommandHandlerResolver>()));

        // Transient operation scope & its provider
        if (!Services.HasService<TransientOperationScopeProvider>()) {
            Services.AddSingleton(c => new TransientOperationScopeProvider(c));
            commander.AddHandlers<TransientOperationScopeProvider>();
        }

        // Nested command logger
        if (!Services.HasService<NestedCommandLogger>()) {
            Services.AddSingleton(c => new NestedCommandLogger(c));
            commander.AddHandlers<NestedCommandLogger>();
        }

        // Operation completion - notifier & producer
        Services.TryAddSingleton(_ => new OperationCompletionNotifier.Options());
        Services.TryAddSingleton<IOperationCompletionNotifier>(c => new OperationCompletionNotifier(
            c.GetRequiredService<OperationCompletionNotifier.Options>(), c));
        Services.TryAddSingleton(_ => new CompletionProducer.Options());
        Services.TryAddEnumerable(ServiceDescriptor.Singleton(
            typeof(IOperationCompletionListener),
            typeof(CompletionProducer)));

        // Command completion handler performing invalidations
        Services.TryAddSingleton(_ => new PostCompletionInvalidator.Options());
        if (!Services.HasService<PostCompletionInvalidator>()) {
            Services.AddSingleton(c => new PostCompletionInvalidator(
                c.GetRequiredService<PostCompletionInvalidator.Options>(), c));
            commander.AddHandlers<PostCompletionInvalidator>();
        }

        // Completion terminator
        if (!Services.HasService<CompletionTerminator>()) {
            Services.AddSingleton(_ => new CompletionTerminator());
            commander.AddHandlers<CompletionTerminator>();
        }

        // Core multitenancy services
        Services.TryAddSingleton<ITenantRegistry<Unit>>(_ => new SingleTenantRegistry<Unit>());
        Services.TryAddSingleton<DefaultTenantResolver<Unit>.Options>();
        Services.TryAddSingleton<ITenantResolver<Unit>>(c => new DefaultTenantResolver<Unit>(
            c.GetRequiredService<DefaultTenantResolver<Unit>.Options>(), c));
        // And make it default
        Services.TryAddSingleton<ITenantRegistry>(c => c.GetRequiredService<ITenantRegistry<Unit>>());
        Services.TryAddSingleton<ITenantResolver>(c => c.GetRequiredService<ITenantResolver<Unit>>());

        configure?.Invoke(this);
    }

    // AddPublisher, AddReplicator

    public FusionBuilder AddPublisher(
        Func<IServiceProvider, PublisherOptions>? optionsFactory = null)
    {
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton(_ => new PublisherOptions());

        Services.TryAddSingleton<IPublisher>(c => new Publisher(c.GetRequiredService<PublisherOptions>(), c));
        return this;
    }

    public FusionBuilder AddReplicator(
        Func<IServiceProvider, ReplicatorOptions>? optionsFactory = null)
    {
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton(_ => new ReplicatorOptions());
        if (Services.HasService<IReplicator>())
            return this;

        // ReplicaCache
        Services.TryAddSingleton<ReplicaCache>(c => new NoReplicaCache(c));

        // Interceptors
        Services.TryAddSingleton(_ => new ReplicaMethodInterceptor.Options());
        Services.TryAddSingleton(c => new ReplicaMethodInterceptor(
            c.GetRequiredService<ReplicaMethodInterceptor.Options>(), c));
        Services.TryAddSingleton(_ => new ReplicaServiceInterceptor.Options());
        Services.TryAddSingleton(c => new ReplicaServiceInterceptor(
            c.GetRequiredService<ReplicaServiceInterceptor.Options>(), c));

        // Replicator
        Services.TryAddSingleton<IReplicator>(c => new Replicator(
            c.GetRequiredService<ReplicatorOptions>(), c));
        return this;
    }

    // AddComputeService

    public FusionBuilder AddComputeService<TService>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class, IComputeService
        => AddComputeService(typeof(TService), typeof(TService), lifetime);
    public FusionBuilder AddComputeService<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService, IComputeService
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
        if (!typeof(IComputeService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(implementationType, nameof(implementationType));
        if (Services.Any(d => d.ServiceType == serviceType))
            return this;

        object Factory(IServiceProvider c)
        {
            // We should try to validate it here because if the type doesn't
            // have any virtual methods (which might be a mistake), no calls
            // will be intercepted, so no error will be thrown later.
            var interceptor = c.GetRequiredService<ComputeServiceInterceptor>();
            interceptor.ValidateType(implementationType);
            return c.ActivateProxy(implementationType, interceptor);
        }

        var descriptor = new ServiceDescriptor(serviceType, Factory, lifetime);
        Services.Add(descriptor);
        Services.AddCommander().AddHandlers(serviceType, implementationType);
        return this;
    }

    // AddAuthentication

    public FusionAuthenticationBuilder AddAuthentication()
        => new(this, null);

    public FusionBuilder AddAuthentication(Action<FusionAuthenticationBuilder> configure) 
        => new FusionAuthenticationBuilder(this, configure).Fusion;

    // AddOperationReprocessor

    public FusionBuilder AddOperationReprocessor(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        => AddOperationReprocessor<OperationReprocessor>(optionsFactory);

    public FusionBuilder AddOperationReprocessor<TOperationReprocessor>(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        where TOperationReprocessor : class, IOperationReprocessor
    {
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton<OperationReprocessorOptions>();

        if (!Services.HasService<IOperationReprocessor>()) {
            Services.AddSingleton(TransientErrorDetector.DefaultPreferNonTransient.For<IOperationReprocessor>());
            Services.AddTransient<TOperationReprocessor>();
            Services.AddTransient<IOperationReprocessor>(c => c.GetRequiredService<TOperationReprocessor>());
            Services.AddCommander().AddHandlers<TOperationReprocessor>();
        }
        return this;
    }

    // AddComputedGraphPruner

    public FusionBuilder AddComputedGraphPruner(
        Func<IServiceProvider, ComputedGraphPruner.Options>? optionsFactory = null)
    {
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton(_ => new ComputedGraphPruner.Options());

        Services.TryAddSingleton(c => new ComputedGraphPruner(
            c.GetRequiredService<ComputedGraphPruner.Options>(), c));
        Services.AddHostedService(c => c.GetRequiredService<ComputedGraphPruner>());
        return this;
    }
}
