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
        Services.TryAddSingleton<Func<Symbol, RpcPeer>>(c => name => new RpcPeer(c.RpcHub(), name));
        Services.TryAddSingleton(c => new RpcPeerResolver(c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(c => new RpcCallFactoryProvider(c));
        Services.TryAddSingleton(_ => new RpcClientInterceptor.Options());
        Services.TryAddTransient(c => new RpcClientInterceptor(c.GetRequiredService<RpcClientInterceptor.Options>(), c));

        // System services
        Services.TryAddSingleton(c => new RpcSystemCallService(c));

        Configuration = GetConfiguration(services);
        HasService<RpcSystemCallService>().Named(RpcSystemCallService.Name);
    }

    public RpcServiceConfiguration HasService<TService>()
        => HasService(typeof(TService));
    public RpcServiceConfiguration HasService(Type serviceType)
    {
        var service = Configuration.Services.GetValueOrDefault(serviceType);
        if (service == null) {
            service = new RpcServiceConfiguration(serviceType);
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

    public RpcBuilder HasConnector(Func<RpcPeer, CancellationToken, Task<Channel<RpcMessage>>> connector)
        => HasConnector(_ => new RpcFuncConnector(connector));

    public RpcBuilder HasConnector<TConnector>(TConnector connector)
        where TConnector : RpcConnector
    {
        Services.AddSingleton(connector);
        if (typeof(TConnector) != typeof(RpcConnector))
            Services.AddSingleton<RpcConnector>(c => c.GetRequiredService<TConnector>());
        return this;
    }

    public RpcBuilder HasConnector<TConnector>(Func<IServiceProvider, TConnector> connectorFactory)
        where TConnector : RpcConnector
    {
        Services.AddSingleton(connectorFactory);
        if (typeof(TConnector) != typeof(RpcConnector))
            Services.AddSingleton<RpcConnector>(c => c.GetRequiredService<TConnector>());
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
