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
        Services.TryAddSingleton<RpcPeerResolver>(c => {
            var hub = c.RpcHub();
            return _ => hub.GetPeer(Symbol.Empty);
        });
        Services.AddSingleton(c => new RpcSystemCallSender(c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(c => new RpcCallFactoryProvider(c));
        Services.TryAddSingleton(_ => new RpcClientInterceptor.Options());
        Services.TryAddTransient(c => new RpcClientInterceptor(c.GetRequiredService<RpcClientInterceptor.Options>(), c));

        // System services
        Services.TryAddSingleton(c => new RpcSystemCalls(c));

        Configuration = GetConfiguration(services);
        HasService<IRpcSystemCalls>(RpcSystemCalls.Name)
            .WithServer<RpcSystemCalls>()
            .WithClient<IRpcSystemCallsClient>();
    }

    public RpcServiceConfiguration HasService<TService>(Symbol name = default)
        => HasService(typeof(TService), name);
    public RpcServiceConfiguration HasService(Type serviceType, Symbol name = default)
    {
        var service = Configuration.Services.GetValueOrDefault(serviceType);
        if (service == null) {
            service = new RpcServiceConfiguration(serviceType, name);
            Configuration.Services.Add(serviceType, service);
        }
        return service;
    }

    public RpcBuilder RemoveService<TService>()
        => RemoveService(typeof(TService));
    public RpcBuilder RemoveService(Type serviceType)
    {
        Configuration.Services.Remove(serviceType);
        return this;
    }

    public RpcBuilder ClearServices()
    {
        Configuration.Services.Clear();
        return this;
    }

    public RpcBuilder RegisterClients()
    {
        foreach (var service in Configuration.Services.Values) {
            var clientType = service.ClientType;
            if (clientType == service.ServerType)
                continue;

            Services.AddSingleton(clientType, c => c.RpcHub().GetClient(clientType));
        }
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
