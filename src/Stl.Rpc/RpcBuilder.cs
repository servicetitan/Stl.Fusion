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
        if (services.Contains(AddedTagDescriptor)) {
            // Already configured
            Configuration = GetConfiguration(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);
        services.AddSingleton(c => new RpcHub(c));

        // Common services
        services.TryAddSingleton(new RpcConfiguration());
        services.TryAddSingleton(c => new RpcServiceRegistry(c));
        services.TryAddSingleton<RpcPeerFactory>(c => RpcPeerFactoryExt.Default(c.RpcHub()));
        services.TryAddSingleton(_ => RpcInboundContext.DefaultFactory);
        services.TryAddSingleton<RpcPeerResolver>(c => {
            var hub = c.RpcHub();
            return (_, _) => hub.GetPeer(Symbol.Empty);
        });

        // Interceptors
        services.TryAddSingleton(_ => new RpcClientInterceptor.Options());
        services.TryAddTransient(c => new RpcClientInterceptor(c.GetRequiredService<RpcClientInterceptor.Options>(), c));
        services.TryAddSingleton(_ => new RpcRoutingInterceptor.Options());
        services.TryAddTransient(c => new RpcRoutingInterceptor(c.GetRequiredService<RpcRoutingInterceptor.Options>(), c));

        Configuration = GetConfiguration(services);

        // System services
        if (!Configuration.Services.ContainsKey(typeof(IRpcSystemCalls))) {
            Service<IRpcSystemCalls>().HasServer<RpcSystemCalls>().HasName(RpcSystemCalls.Name);
            services.TryAddSingleton(c => new RpcSystemCalls(c));
            services.TryAddSingleton(c => new RpcSystemCallSender(c));
        }
    }

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

    public RpcBuilder HasClientChannelProvider(RpcClientChannelProvider clientChannelProvider)
    {
        Services.AddSingleton(clientChannelProvider);
        return this;
    }

    public RpcBuilder HasClientChannelProvider(Func<IServiceProvider, RpcClientChannelProvider> connectorFactory)
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
