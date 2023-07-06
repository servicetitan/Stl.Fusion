using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.OS;
using Stl.Rpc.Clients;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public readonly struct RpcBuilder
{
    public IServiceCollection Services { get; }
    public RpcConfiguration Configuration { get; }

    internal RpcBuilder(
        IServiceCollection services,
        Action<RpcBuilder>? configure)
    {
        Services = services;
        if (GetConfiguration(services) is { } configuration) {
            // Already configured
            Configuration = configuration;
            configure?.Invoke(this);
            return;
        }

        // We want above GetConfiguration call to run in O(1), so...
        Configuration = new RpcConfiguration();
        services.Insert(0, new ServiceDescriptor(typeof(RpcConfiguration), Configuration));
        services.AddSingleton(c => new RpcHub(c));

        // Common services
        services.TryAddSingleton(c => new RpcServiceRegistry(c));
        services.TryAddSingleton(_ => RpcDefaultDelegates.ServiceNameBuilder);
        services.TryAddSingleton(_ => RpcDefaultDelegates.MethodNameBuilder);
        services.TryAddSingleton(_ => RpcDefaultDelegates.CallRouter);
        services.TryAddSingleton(_ => RpcDefaultDelegates.InboundContextFactory);
        services.TryAddSingleton(_ => RpcDefaultDelegates.PeerFactory);
        services.TryAddSingleton(_ => RpcDefaultDelegates.ClientIdGenerator);
        services.TryAddSingleton(_ => RpcDefaultDelegates.BackendServiceDetector);
        services.TryAddSingleton(_ => RpcDefaultDelegates.UnrecoverableErrorDetector);
        services.TryAddSingleton(_ => RpcDefaultDelegates.ClientConnectionFactory);
        services.TryAddSingleton(_ => RpcDefaultDelegates.ServerConnectionFactory);
        services.TryAddSingleton(_ => RpcArgumentSerializer.Default);
        services.TryAddSingleton(c => new RpcInboundMiddlewares(c));
        services.TryAddSingleton(c => new RpcOutboundMiddlewares(c));
        services.TryAddTransient(_ => new RpcInboundCallTracker());
        services.TryAddTransient(_ => new RpcOutboundCallTracker());
        services.TryAddSingleton(c => new RpcClientPeerReconnectDelayer(c));
        if (!OSInfo.IsAnyClient)
            AddInboundMiddleware<RpcInboundCallActivityMiddleware>();

        // Interceptors
        services.TryAddSingleton(_ => RpcClientInterceptor.Options.Default);
        services.TryAddTransient(c => new RpcClientInterceptor(c.GetRequiredService<RpcClientInterceptor.Options>(), c));
        services.TryAddSingleton(_ => RpcRoutingInterceptor.Options.Default);
        services.TryAddTransient(c => new RpcRoutingInterceptor(c.GetRequiredService<RpcRoutingInterceptor.Options>(), c));

        // System services
        if (!Configuration.Services.ContainsKey(typeof(IRpcSystemCalls))) {
            Service<IRpcSystemCalls>().HasServer<RpcSystemCalls>().HasName(RpcSystemCalls.Name);
            services.TryAddSingleton(c => new RpcSystemCalls(c));
            services.TryAddSingleton(c => new RpcSystemCallSender(c));
        }
    }

    // WebSocket client

    public RpcBuilder AddWebSocketClient(Uri hostUri)
        => AddWebSocketClient(_ => hostUri.ToString());

    public RpcBuilder AddWebSocketClient(string hostUrl)
        => AddWebSocketClient(_ => hostUrl);

    public RpcBuilder AddWebSocketClient(Func<IServiceProvider, string> hostUrlFactory)
        => AddWebSocketClient(c => RpcWebSocketClient.Options.Default with {
            HostUrlResolver = (_, _) => hostUrlFactory.Invoke(c),
        });

    public RpcBuilder AddWebSocketClient(Func<IServiceProvider, RpcWebSocketClient.Options>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => RpcWebSocketClient.Options.Default);
        if (services.HasService<RpcWebSocketClient>())
            return this;

        services.AddSingleton(c => new RpcWebSocketClient(
            c.GetRequiredService<RpcWebSocketClient.Options>(), c));
        services.AddAlias<RpcClient, RpcWebSocketClient>();
        services.AddTransient(_ => new ClientWebSocket());
        return this;
    }

    // Share, Connect, Route

    public RpcBuilder AddService<TService>(RpcServiceMode mode, Symbol name = default)
        where TService : class
        => AddService(typeof(TService), mode, name);
    public RpcBuilder AddService<TService, TServer>(RpcServiceMode mode, Symbol name = default)
        where TService : class
        where TServer : class, TService
        => AddService(typeof(TService), typeof(TServer), mode, name);
    public RpcBuilder AddService(Type serviceType, RpcServiceMode mode, Symbol name = default)
        => AddService(serviceType, serviceType, mode, name);
    public RpcBuilder AddService(Type serviceType, Type serverType, RpcServiceMode mode, Symbol name = default)
        => mode switch {
            RpcServiceMode.Server => AddServer(serviceType, serverType, name),
            RpcServiceMode.Router => AddRouter(serviceType, serverType, name),
            RpcServiceMode.ServingRouter => AddRouter(serviceType, serverType).AddServer(serviceType, name),
            RpcServiceMode.RoutingServer => AddRouter(serviceType, serverType).AddServer(serviceType, serverType, name),
            _ => Service(serverType).HasName(name).Rpc,
        };

    public RpcBuilder AddServer<TService>(Symbol name = default)
        where TService : class
        => AddServer(typeof(TService), name);
    public RpcBuilder AddServer<TService, TServer>(Symbol name = default)
        where TService : class
        where TServer : class, TService
        => AddServer(typeof(TService), typeof(TServer), name);
    public RpcBuilder AddServer(Type serviceType, Symbol name = default)
        => AddServer(serviceType, serviceType, name);
    public RpcBuilder AddServer(Type serviceType, Type serverType, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!typeof(IRpcService).IsAssignableFrom(serviceType))
            throw Stl.Internal.Errors.MustImplement<IRpcService>(serviceType, nameof(serviceType));
        if (!serviceType.IsAssignableFrom(serverType))
            throw Stl.Internal.Errors.MustBeAssignableTo(serverType, serviceType, nameof(serverType));

        Service(serviceType).HasServer(serverType).HasName(name);
        if (!serverType.IsInterface)
            Services.AddSingleton(serverType);
        return this;
    }

    public RpcBuilder AddClient<TService>(Symbol name = default)
        where TService : class
        => AddClient(typeof(TService), name);
    public RpcBuilder AddClient<TService, TClient>(Symbol name = default)
        where TService : class
        where TClient : class, TService
        => AddClient(typeof(TService), typeof(TClient), name);
    public RpcBuilder AddClient(Type serviceType, Symbol name = default)
        => AddClient(serviceType, serviceType, name);
    public RpcBuilder AddClient(Type serviceType, Type clientType, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!typeof(IRpcService).IsAssignableFrom(serviceType))
            throw Stl.Internal.Errors.MustImplement<IRpcService>(serviceType, nameof(serviceType));
        if (!serviceType.IsAssignableFrom(clientType))
            throw Stl.Internal.Errors.MustBeAssignableTo(clientType, serviceType, nameof(clientType));

        Service(serviceType).HasName(name);
        Services.AddSingleton(clientType, c => RpcProxies.NewClientProxy(c, serviceType, clientType));
        return this;
    }

    public RpcBuilder AddRouter<TService, TServer>(Symbol name = default)
        where TService : class
        where TServer : class, TService
        => AddRouter(typeof(TService), typeof(TServer), name);
    public RpcBuilder AddRouter(Type serviceType, Type serverType, Symbol name = default)
        => serviceType == serverType
            ? throw new ArgumentOutOfRangeException(nameof(serverType))
            : AddRouter(serviceType, ServiceResolver.New(serverType), name);
    public RpcBuilder AddRouter(Type serviceType, ServiceResolver serverResolver, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!typeof(IRpcService).IsAssignableFrom(serviceType))
            throw Stl.Internal.Errors.MustImplement<IRpcService>(serviceType, nameof(serviceType));

        Service(serviceType).HasName(name);
        Services.AddSingleton(serviceType, c => RpcProxies.NewRoutingProxy(c, serviceType, serverResolver));
        return this;
    }

    // More low-level configuration options stuff

    public RpcServiceBuilder Service<TService>()
        => Service(typeof(TService));

    public RpcServiceBuilder Service(Type serviceType)
    {
        if (Configuration.Services.TryGetValue(serviceType, out var service))
            return service;

        service = new RpcServiceBuilder(this, serviceType);
        Configuration.Services.Add(serviceType, service);
        return service;
    }

    public RpcBuilder AddInboundMiddleware<TMiddleware>()
        where TMiddleware : RpcInboundMiddleware
        => AddInboundMiddleware(typeof(TMiddleware));

    public RpcBuilder AddInboundMiddleware(Type middlewareType)
    {
        if (!typeof(RpcInboundMiddleware).IsAssignableFrom(middlewareType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcInboundMiddleware>(middlewareType, nameof(middlewareType));

        var descriptor = ServiceDescriptor.Singleton(typeof(RpcInboundMiddleware), middlewareType);
        Services.TryAddEnumerable(descriptor);
        return this;
    }

    public RpcBuilder RemoveInboundMiddleware<TMiddleware>()
        where TMiddleware : RpcInboundMiddleware
        => RemoveInboundMiddleware(typeof(TMiddleware));

    public RpcBuilder RemoveInboundMiddleware(Type middlewareType)
    {
        if (!typeof(RpcInboundMiddleware).IsAssignableFrom(middlewareType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcInboundMiddleware>(middlewareType, nameof(middlewareType));

        Services.RemoveAll(d =>
            d.ImplementationType == middlewareType
            && d.ServiceType == typeof(RpcInboundMiddleware));
        return this;
    }

    public RpcBuilder AddOutboundMiddleware<TMiddleware>()
        where TMiddleware : RpcOutboundMiddleware
        => AddOutboundMiddleware(typeof(TMiddleware));

    public RpcBuilder AddOutboundMiddleware(Type middlewareType)
    {
        if (!typeof(RpcOutboundMiddleware).IsAssignableFrom(middlewareType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcOutboundMiddleware>(middlewareType, nameof(middlewareType));

        var descriptor = ServiceDescriptor.Singleton(typeof(RpcOutboundMiddleware), middlewareType);
        Services.TryAddEnumerable(descriptor);
        return this;
    }

    public RpcBuilder RemoveOutboundMiddleware<TMiddleware>()
        where TMiddleware : RpcOutboundMiddleware
        => RemoveOutboundMiddleware(typeof(TMiddleware));

    public RpcBuilder RemoveOutboundMiddleware(Type middlewareType)
    {
        if (!typeof(RpcOutboundMiddleware).IsAssignableFrom(middlewareType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcOutboundMiddleware>(middlewareType, nameof(middlewareType));

        Services.RemoveAll(d =>
            d.ImplementationType == middlewareType
            && d.ServiceType == typeof(RpcOutboundMiddleware));
        return this;
    }

    // Private methods

    private static RpcConfiguration? GetConfiguration(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(RpcConfiguration)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list to speed up future lookups
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (RpcConfiguration?)descriptor.ImplementationInstance
                    ?? throw Errors.RpcOptionsMustBeRegisteredAsInstance();
            }
        }
        return null;
    }
}
