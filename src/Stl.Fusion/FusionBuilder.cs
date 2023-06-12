using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Fusion.Multitenancy;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Fusion.Rpc.Cache;
using Stl.Fusion.Rpc.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Fusion.UI;
using Stl.Interception;
using Stl.Multitenancy;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Versioning.Providers;

namespace Stl.Fusion;

public readonly struct FusionBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public RpcBuilder Rpc { get;}

    internal FusionBuilder(
        IServiceCollection services,
        Action<FusionBuilder>? configure)
    {
        Services = services;
        Rpc = services.AddRpc();
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
        services.TryAddSingleton(_ => new ComputeServiceInterceptor.Options());
        services.TryAddSingleton(c => new ComputeServiceInterceptor(
            c.GetRequiredService<ComputeServiceInterceptor.Options>(), c));

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

        // Core authentication services
        services.TryAddScoped<ISessionResolver>(c => new SessionResolver(c));
        services.TryAddScoped(c => c.GetRequiredService<ISessionResolver>().Session);
        services.TryAddSingleton<ISessionFactory>(_ => new SessionFactory());

        // Core multitenancy services
        services.TryAddSingleton<ITenantRegistry<Unit>>(_ => new SingleTenantRegistry<Unit>());
        services.TryAddSingleton<DefaultTenantResolver<Unit>.Options>();
        services.TryAddSingleton<ITenantResolver<Unit>>(c => new DefaultTenantResolver<Unit>(
            c.GetRequiredService<DefaultTenantResolver<Unit>.Options>(), c));
        // And make it default
        services.TryAddAlias<ITenantRegistry, ITenantRegistry<Unit>>();
        services.TryAddAlias<ITenantResolver, ITenantResolver<Unit>>();

        // RPC

        // Compute system calls service + call type
        if (!Rpc.Configuration.Services.ContainsKey(typeof(IRpcComputeSystemCalls))) {
            Rpc.Service<IRpcComputeSystemCalls>().HasServer<RpcComputeSystemCalls>().HasName(RpcComputeSystemCalls.Name);
            services.AddSingleton(c => new RpcComputeSystemCalls(c));
            services.AddSingleton(c => new RpcComputeSystemCallSender(c));
            Rpc.Configuration.InboundCallTypes.Add(
                RpcComputeCall.CallTypeId,
                typeof(RpcInboundComputeCall<>));
        }

        // Compute call interceptor
        services.TryAddSingleton(_ => new RpcComputeServiceInterceptor.Options());
        services.TryAddSingleton(c => new RpcComputeServiceInterceptor(
            c.GetRequiredService<RpcComputeServiceInterceptor.Options>(), c));

        // Compute call cache
        services.AddSingleton(c => (RpcComputedCache)new RpcNoComputedCache(c));

        configure?.Invoke(this);
    }

    // ComputeService

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

    public FusionBuilder AddComputeServer<TService, TImplementation>(Symbol name = default)
        => AddComputeServer(typeof(TService), typeof(TImplementation), name);
    public FusionBuilder AddComputeServer(Type serviceType, Type implementationType, Symbol name = default)
    {
        AddComputeService(serviceType, implementationType);
        Rpc.Service(serviceType).HasServer(implementationType).HasName(name);
        return this;
    }

    public FusionBuilder AddComputeClient<TService>(Symbol name = default)
        => AddComputeClient(typeof(TService), name);
    public FusionBuilder AddComputeClient(Type serviceType, Symbol name = default)
    {
        Rpc.Service(serviceType).HasName(name);
        Services.AddSingleton(serviceType, c => {
            var rpcHub = c.RpcHub();
            var client = rpcHub.CreateClient(serviceType);

            var replicaServiceInterceptor = c.GetRequiredService<RpcComputeServiceInterceptor>();
            var clientProxy = Proxies.New(serviceType, replicaServiceInterceptor, client);
            return clientProxy;
        });
        return this;
    }

    public FusionBuilder AddComputeRouter<TService, TServer>(Symbol name = default)
        => AddComputeRouter(typeof(TService), typeof(TServer), name);
    public FusionBuilder AddComputeRouter(Type serviceType, Type serverType, Symbol name = default)
    {
        AddComputeService(serviceType, serverType);
        Services.AddSingleton(serviceType, c => {
            var rpcHub = c.RpcHub();
            var server = rpcHub.ServiceRegistry[serviceType].Server;
            var client = rpcHub.CreateClient(serviceType);

            var replicaServiceInterceptor = c.GetRequiredService<RpcComputeServiceInterceptor>();
            var clientProxy = Proxies.New(serviceType, replicaServiceInterceptor, client);

            var routingInterceptor = c.GetRequiredService<RpcRoutingInterceptor>();
            var serviceDef = rpcHub.ServiceRegistry[serviceType];
            routingInterceptor.Setup(serviceDef, server, clientProxy);
            var routingProxy = Proxies.New(serviceType, routingInterceptor);
            return routingProxy;
        });
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
        var services = Services;
        services.AddSingleton(optionsFactory, _ => OperationReprocessorOptions.Default);
        if (services.HasService<TOperationReprocessor>())
            return this;

        services.AddTransient<TOperationReprocessor>();
        services.AddAlias<IOperationReprocessor, TOperationReprocessor>(ServiceLifetime.Transient);
        services.AddCommander().AddHandlers<TOperationReprocessor>();
        services.AddSingleton(TransientErrorDetector.DefaultPreferNonTransient.For<IOperationReprocessor>());
        return this;
    }

    // AddComputedGraphPruner

    public FusionBuilder AddComputedGraphPruner(
        Func<IServiceProvider, ComputedGraphPruner.Options>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => ComputedGraphPruner.Options.Default);
        if (services.HasService<ComputedGraphPruner>())
            return this;

        services.AddSingleton(c => new ComputedGraphPruner(
            c.GetRequiredService<ComputedGraphPruner.Options>(), c));
        services.AddHostedService(c => c.GetRequiredService<ComputedGraphPruner>());
        return this;
    }
}
