using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public readonly struct RpcBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public RpcConfiguration Configuration { get; }

    internal RpcBuilder(
        IServiceCollection services,
        Action<RpcBuilder>? configure)
    {
        Services = services;
        if (Services.Contains(AddedTagDescriptor)) {
            // Already configured
            Configuration = GetConfiguration(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);

        // Common services
        Services.TryAddSingleton(new RpcConfiguration());
        Services.TryAddSingleton(c => new RpcHub(c));
        Services.TryAddSingleton<RpcPeerFactory>(c => name => new RpcPeer(c.RpcHub(), name));
        Services.TryAddSingleton(_ => RpcInboundContext.DefaultFactory);
        Services.TryAddSingleton<RpcPeerResolver>(c => {
            var hub = c.RpcHub();
            return _ => hub.GetPeer(Symbol.Empty);
        });
        Services.AddSingleton(c => new RpcSystemCallSender(c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(_ => new RpcClientInterceptor.Options());
        Services.TryAddTransient(c => new RpcClientInterceptor(c.GetRequiredService<RpcClientInterceptor.Options>(), c));

        Configuration = GetConfiguration(services);

        // System services
        if (!Configuration.Services.ContainsKey(typeof(IRpcSystemCalls))) {
            Services.AddSingleton(c => new RpcSystemCalls(c));
            AddService<IRpcSystemCalls, RpcSystemCalls>(RpcSystemCalls.Name);
        }
    }

    public RpcBuilder AddService<TService>(
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        => AddService<TService, TService>(default, serviceConfigurationBuilder);

    public RpcBuilder AddService<TService>(
        Symbol name,
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        => AddService<TService, TService>(name, serviceConfigurationBuilder);

    public RpcBuilder AddService<TService, TServer>(
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        where TServer : TService
        => AddService<TService, TServer, TService>(default, serviceConfigurationBuilder);

    public RpcBuilder AddService<TService, TServer>(
        Symbol name,
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        where TServer : TService
        => AddService<TService, TServer, TService>(name, serviceConfigurationBuilder);

    public RpcBuilder AddService<TService, TServer, TClient>(
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        where TServer : TService
        where TClient : TService
        => AddService<TService, TServer, TClient>(default, serviceConfigurationBuilder);

    public RpcBuilder AddService<TService, TServer, TClient>(
        Symbol name,
        Func<RpcServiceConfiguration<TService>, RpcServiceConfiguration<TService>>? serviceConfigurationBuilder = null)
        where TServer : TService
        where TClient : TService
    {
        var serviceType = typeof(TService);
        if (Configuration.Services.ContainsKey(serviceType))
            throw Errors.ServiceAlreadyExists(serviceType);

        var cfg = new RpcServiceConfiguration<TService>(typeof(TServer), typeof(TClient), name);
        if (serviceConfigurationBuilder != null)
            cfg = serviceConfigurationBuilder.Invoke(cfg);
        cfg.RequireValid(serviceType);

        Configuration.Services[serviceType] = cfg;
        if (cfg.ClientType != cfg.ServerType)
            Services.AddSingleton(cfg.ClientType, c => c.RpcHub().GetClient(cfg.ClientType));
        return this;
    }

    public RpcBuilder AddService(
        Type serviceType,
        Func<RpcServiceConfiguration, RpcServiceConfiguration>? serviceConfigurationBuilder = null)
    {
        if (Configuration.Services.ContainsKey(serviceType))
            throw Errors.ServiceAlreadyExists(serviceType);

        var cfg = new RpcServiceConfiguration(serviceType);
        if (serviceConfigurationBuilder != null)
            cfg = serviceConfigurationBuilder.Invoke(cfg);
        cfg.RequireValid(serviceType);

        Configuration.Services[serviceType] = cfg;
        if (cfg.ClientType != cfg.ServerType)
            Services.AddSingleton(cfg.ClientType, c => c.RpcHub().GetClient(cfg.ClientType));
        return this;
    }

    public RpcBuilder RemoveService<TService>()
        => RemoveService(typeof(TService));
    public RpcBuilder RemoveService(Type serviceType)
    {
        if (!Configuration.Services.TryGetValue(serviceType, out var cfg))
            return this;

        Configuration.Services.Remove(serviceType);
        if (cfg.ClientType != cfg.ServerType)
            Services.RemoveAll(cfg.ClientType);
        return this;
    }

    public RpcBuilder HasPeerFactory(RpcPeerFactory peerFactory)
    {
        Services.AddSingleton(peerFactory);
        return this;
    }

    public RpcBuilder HasPeerFactory(Func<IServiceProvider, RpcPeerFactory> peerFactoryFactory)
    {
        Services.AddSingleton(peerFactoryFactory);
        return this;
    }

    public RpcBuilder HasInboundContextFactory(RpcInboundContextFactory inboundContextFactory)
    {
        Services.AddSingleton(inboundContextFactory);
        return this;
    }

    public RpcBuilder HasInboundContextFactory(Func<IServiceProvider, RpcInboundContextFactory> inboundContextFactoryFactory)
    {
        Services.AddSingleton(inboundContextFactoryFactory);
        return this;
    }

    public RpcBuilder HasPeerConnector(RpcPeerConnector peerConnector)
    {
        Services.AddSingleton(peerConnector);
        return this;
    }

    public RpcBuilder HasPeerConnector(Func<IServiceProvider, RpcPeerConnector> connectorFactory)
    {
        Services.AddSingleton(connectorFactory);
        return this;
    }

    public RpcBuilder HasPeerResolver(RpcPeerResolver peerResolver)
    {
        Services.AddSingleton(peerResolver);
        return this;
    }

    public RpcBuilder HasPeerResolver(Func<IServiceProvider, RpcPeerResolver> peerResolverFactory)
    {
        Services.AddSingleton(peerResolverFactory);
        return this;
    }

    // Private methods

    private static RpcConfiguration GetConfiguration(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(RpcConfiguration)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list
                    // to speed up future searches
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (RpcConfiguration?) descriptor.ImplementationInstance
                    ?? throw Errors.RpcOptionsMustBeRegisteredAsInstance();
            }
        }
        throw Errors.RpcOptionsIsNotRegistered();
    }
}
