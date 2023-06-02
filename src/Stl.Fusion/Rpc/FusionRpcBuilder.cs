using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Rpc.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc;

[StructLayout(LayoutKind.Auto)]
public readonly struct FusionRpcBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public FusionBuilder Fusion { get; }
    public RpcBuilder Rpc { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionRpcBuilder(
        FusionBuilder fusion,
        Action<FusionRpcBuilder>? configure)
    {
        Fusion = fusion;
        var rpc = Rpc = fusion.Services.AddRpc();
        var services = Services;
        if (services.Contains(AddedTagDescriptor)) {
            // Already configured
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);

        // Compute system calls service + call type
        if (!rpc.Configuration.Services.ContainsKey(typeof(IRpcComputeSystemCalls))) {
            rpc.Service<IRpcComputeSystemCalls>().HasServer<RpcComputeSystemCalls>().HasName(RpcComputeSystemCalls.Name);
            services.AddSingleton(c => new RpcComputeSystemCalls(c));
            services.AddSingleton(c => new RpcComputeSystemCallSender(c));
            rpc.Configuration.InboundCallTypes.Add(
                RpcComputeCall.CallTypeId,
                typeof(RpcInboundComputeCall<>));
        }

        // Compute call interceptor
        services.TryAddSingleton(_ => new RpcComputeServiceInterceptor.Options());
        services.TryAddSingleton(c => new RpcComputeServiceInterceptor(
            c.GetRequiredService<RpcComputeServiceInterceptor.Options>(), c));

        // Compute call cache
        services.AddSingleton(c => (RpcComputedCache)new RpcNoComputedCache(c));
    }

    public FusionRpcBuilder AddServer<TService, TServer>(Symbol name = default)
        => AddServer(typeof(TService), typeof(TServer), name);
    public FusionRpcBuilder AddServer(Type serviceType, Type serverType, Symbol name = default)
    {
        Rpc.Service(serviceType).HasServer(serverType).HasName(name);
        return this;
    }

    public FusionRpcBuilder AddClient<TService>(Symbol name = default)
        => AddClient(typeof(TService), name);
    public FusionRpcBuilder AddClient(Type serviceType, Symbol name = default)
    {
        Rpc.Service(serviceType).HasName(name);
        Services.AddSingleton(serviceType, c => {
            var rpcHub = c.RpcHub();
            var rpcClient = rpcHub.CreateClient(serviceType);

            var computeServiceInterceptor = c.GetRequiredService<RpcComputeServiceInterceptor>();
            var clientProxy = Proxies.New(serviceType, computeServiceInterceptor, rpcClient);
            return clientProxy;
        });
        return this;
    }

    public FusionRpcBuilder AddRouter<TService, TServer>(Symbol name = default)
        => AddRouter(typeof(TService), typeof(TServer), name);
    public FusionRpcBuilder AddRouter(Type serviceType, Type serverType, Symbol name = default)
    {
        AddServer(serviceType, serverType, name);
        Services.AddSingleton(serviceType, c => {
            var rpcHub = c.RpcHub();
            var server = rpcHub.ServiceRegistry[serviceType].Server;
            var rpcClient = rpcHub.CreateClient(serviceType);

            var computeServiceInterceptor = c.GetRequiredService<RpcComputeServiceInterceptor>();
            var clientProxy = Proxies.New(serviceType, computeServiceInterceptor, rpcClient);

            var routingInterceptor = c.GetRequiredService<RpcRoutingInterceptor>();
            var serviceDef = rpcHub.ServiceRegistry[serviceType];
            routingInterceptor.Setup(serviceDef, server, clientProxy);
            var routingProxy = Proxies.New(serviceType, routingInterceptor);
            return routingProxy;
        });
        return this;
    }
}
