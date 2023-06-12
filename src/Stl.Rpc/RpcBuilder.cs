using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Interception;
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
        services.TryAddSingleton(_ => RpcDefaults.ServiceNameBuilder);
        services.TryAddSingleton(_ => RpcDefaults.MethodNameBuilder);
        services.TryAddSingleton(_ => RpcDefaults.PeerFactory);
        services.TryAddSingleton(_ => RpcDefaults.CallRouter);
        services.TryAddSingleton(_ => RpcDefaults.InboundContextFactory);
        services.TryAddSingleton(_ => RpcDefaults.ArgumentSerializer);
        services.TryAddSingleton(_ => RpcDefaults.ClientIdGenerator);
        services.TryAddSingleton(_ => RpcDefaults.BackendServiceDetector);
        services.TryAddSingleton(_ => RpcDefaults.UnrecoverableErrorDetector);
        services.TryAddSingleton(_ => RpcDefaults.ClientChannelFactory);

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

    public RpcBuilder UseWebSocketClient(string hostUrl)
        => UseWebSocketClient(_ => hostUrl);

    public RpcBuilder UseWebSocketClient(Func<IServiceProvider, string> hostUrlFactory)
        => UseWebSocketClient(c => RpcWebSocketClient.Options.Default with {
            HostUrlResolver = (_, _) => hostUrlFactory.Invoke(c),
        });

    public RpcBuilder UseWebSocketClient(Func<IServiceProvider, RpcWebSocketClient.Options>? optionsFactory = null)
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

    public RpcBuilder AddServer<TService>(Symbol name = default)
        => AddServer(typeof(TService), typeof(TService), name);
    public RpcBuilder AddServer<TService, TServer>(Symbol name = default)
        => AddServer(typeof(TService), typeof(TServer), name);
    public RpcBuilder AddServer(Type serviceType, Type serverType, Symbol name = default)
    {
        Service(serviceType).HasServer(serverType).HasName(name);
        return this;
    }

    public RpcBuilder AddClient<TService>(Symbol name = default)
        => AddClient(typeof(TService), name);
    public RpcBuilder AddClient(Type serviceType, Symbol name = default)
    {
        Service(serviceType).HasClient().HasName(name);
        return this;
    }

    public RpcBuilder AddRouter<TService, TServer>(Symbol name = default)
        => AddRouter(typeof(TService), typeof(TServer), name);
    public RpcBuilder AddRouter(Type serviceType, Type serverType, Symbol name = default)
    {
        AddServer(serviceType, serverType, name);
        Services.AddSingleton(serviceType, c => {
            var rpcHub = c.RpcHub();
            var server = rpcHub.ServiceRegistry[serviceType].Server;
            var client = rpcHub.CreateClient(serviceType);

            var routingInterceptor = c.GetRequiredService<RpcRoutingInterceptor>();
            var serviceDef = rpcHub.ServiceRegistry[serviceType];
            routingInterceptor.Setup(serviceDef, server, client);
            var routingProxy = Proxies.New(serviceType, routingInterceptor);
            return routingProxy;
        });
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
