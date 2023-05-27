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
        if (services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);
        services.AddCommander();

        // Common services
        services.AddOptions();
        services.AddConverters();
        services.TryAddSingleton(_ => MomentClockSet.Default);
        services.TryAddSingleton(c => c.GetRequiredService<MomentClockSet>().SystemClock);
        services.TryAddSingleton(_ => LTagVersionGenerator.Default);
        services.TryAddSingleton(_ => ClockBasedVersionGenerator.DefaultCoarse);

        // Compute services & their dependencies
        services.TryAddSingleton(_ => new ComputedOptionsProvider());
        services.TryAddSingleton(_ => TransientErrorDetector.DefaultPreferTransient.For<IComputed>());
        services.TryAddSingleton(_ => new ComputeMethodInterceptor.Options());
        services.TryAddSingleton(c => new ComputeMethodInterceptor(
            c.GetRequiredService<ComputeMethodInterceptor.Options>(), c));

        // States
        services.TryAddSingleton(c => new MixedModeService<IStateFactory>.Singleton(new StateFactory(c), c));
        services.TryAddScoped(c => new MixedModeService<IStateFactory>.Scoped(new StateFactory(c), c));
        services.TryAddTransient(c => c.GetRequiredMixedModeService<IStateFactory>());
        services.TryAddSingleton(typeof(MutableState<>.Options));
        services.TryAddTransient(typeof(IMutableState<>), typeof(MutableState<>));

        // Update delayer & UI action tracker
        services.TryAddSingleton(_ => new UIActionTracker.Options());
        services.TryAddScoped<UIActionTracker>(c => new UIActionTracker(
            c.GetRequiredService<UIActionTracker.Options>(), c));
        services.TryAddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker()));

        // CommandR, command completion and invalidation
        var commander = services.AddCommander();
        services.TryAddSingleton(_ => new AgentInfo());
        services.TryAddSingleton(c => new InvalidationInfoProvider(
            c.Commander(), c.GetRequiredService<CommandHandlerResolver>()));

        // Transient operation scope & its provider
        if (!services.HasService<TransientOperationScopeProvider>()) {
            services.AddSingleton(c => new TransientOperationScopeProvider(c));
            commander.AddHandlers<TransientOperationScopeProvider>();
        }

        // Nested command logger
        if (!services.HasService<NestedCommandLogger>()) {
            services.AddSingleton(c => new NestedCommandLogger(c));
            commander.AddHandlers<NestedCommandLogger>();
        }

        // Operation completion - notifier & producer
        services.TryAddSingleton(_ => new OperationCompletionNotifier.Options());
        services.TryAddSingleton<IOperationCompletionNotifier>(c => new OperationCompletionNotifier(
            c.GetRequiredService<OperationCompletionNotifier.Options>(), c));
        services.TryAddSingleton(_ => new CompletionProducer.Options());
        services.TryAddEnumerable(ServiceDescriptor.Singleton(
            typeof(IOperationCompletionListener),
            typeof(CompletionProducer)));

        // Command completion handler performing invalidations
        services.TryAddSingleton(_ => new PostCompletionInvalidator.Options());
        if (!services.HasService<PostCompletionInvalidator>()) {
            services.AddSingleton(c => new PostCompletionInvalidator(
                c.GetRequiredService<PostCompletionInvalidator.Options>(), c));
            commander.AddHandlers<PostCompletionInvalidator>();
        }

        // Completion terminator
        if (!services.HasService<CompletionTerminator>()) {
            services.AddSingleton(_ => new CompletionTerminator());
            commander.AddHandlers<CompletionTerminator>();
        }

        // Core multitenancy services
        services.TryAddSingleton<ITenantRegistry<Unit>>(_ => new SingleTenantRegistry<Unit>());
        services.TryAddSingleton<DefaultTenantResolver<Unit>.Options>();
        services.TryAddSingleton<ITenantResolver<Unit>>(c => new DefaultTenantResolver<Unit>(
            c.GetRequiredService<DefaultTenantResolver<Unit>.Options>(), c));
        // And make it default
        services.TryAddSingleton<ITenantRegistry>(c => c.GetRequiredService<ITenantRegistry<Unit>>());
        services.TryAddSingleton<ITenantResolver>(c => c.GetRequiredService<ITenantResolver<Unit>>());

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
        var services = Services;
        if (optionsFactory != null)
            services.AddSingleton(optionsFactory);
        else
            services.TryAddSingleton(_ => new ReplicatorOptions());
        if (services.HasService<IReplicator>())
            return this;

        // ReplicaCache
        services.TryAddSingleton<ReplicaCache>(c => new NoReplicaCache(c));

        // Interceptors
        services.TryAddSingleton(_ => new ReplicaMethodInterceptor.Options());
        services.TryAddSingleton(c => new ReplicaMethodInterceptor(
            c.GetRequiredService<ReplicaMethodInterceptor.Options>(), c));

        // Replicator
        services.TryAddSingleton<IReplicator>(c => new Replicator(
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
            var interceptor = c.GetRequiredService<ComputeMethodInterceptor>();
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
        var services = Services;
        if (optionsFactory != null)
            services.AddSingleton(optionsFactory);
        else
            services.TryAddSingleton<OperationReprocessorOptions>();

        if (!services.HasService<IOperationReprocessor>()) {
            services.AddSingleton(TransientErrorDetector.DefaultPreferNonTransient.For<IOperationReprocessor>());
            services.AddTransient<TOperationReprocessor>();
            services.AddTransient<IOperationReprocessor>(c => c.GetRequiredService<TOperationReprocessor>());
            services.AddCommander().AddHandlers<TOperationReprocessor>();
        }
        return this;
    }

    // AddComputedGraphPruner

    public FusionBuilder AddComputedGraphPruner(
        Func<IServiceProvider, ComputedGraphPruner.Options>? optionsFactory = null)
    {
        var services = Services;
        if (optionsFactory != null)
            services.AddSingleton(optionsFactory);
        else
            services.TryAddSingleton(_ => new ComputedGraphPruner.Options());

        services.TryAddSingleton(c => new ComputedGraphPruner(
            c.GetRequiredService<ComputedGraphPruner.Options>(), c));
        services.AddHostedService(c => c.GetRequiredService<ComputedGraphPruner>());
        return this;
    }
}
