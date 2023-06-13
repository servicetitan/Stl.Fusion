using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Interception;
using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Fusion.Internal;

public static class FusionProxies
{
    public static object NewServiceProxy(IServiceProvider services, Type serviceType)
    {
        // We should try to validate it here because if the type doesn't
        // have any virtual methods (which might be a mistake), no calls
        // will be intercepted, so no error will be thrown later.
        var interceptor = services.GetRequiredService<ComputeServiceInterceptor>();
        interceptor.ValidateType(serviceType);
        var serviceProxy = services.ActivateProxy(serviceType, interceptor);
        return serviceProxy;
    }

    public static object NewClientProxy(IServiceProvider services, Type serviceType)
    {
        var rpcHub = services.RpcHub();
        var client = RpcProxies.NewClientProxy(services, serviceType, serviceType);
        var serviceDef = rpcHub.ServiceRegistry[serviceType];

        var clientInterceptor = services.GetRequiredService<RpcComputeClientInterceptor>();
        clientInterceptor.Setup(serviceDef);
        clientInterceptor.ValidateType(serviceType);
        var clientProxy = Proxies.New(serviceType, clientInterceptor, client);
        return clientProxy;
    }

    public static object NewRoutingProxy(IServiceProvider services, Type serviceType, ServiceResolver serverResolver)
    {
        var rpcHub = services.RpcHub();
        var server = serverResolver.Resolve(services);
        var client = NewClientProxy(services, serviceType);
        var serviceDef = rpcHub.ServiceRegistry[serviceType];

        var routingInterceptor = services.GetRequiredService<RpcRoutingInterceptor>();
        routingInterceptor.Setup(serviceDef, server, client);
        var routingProxy = Proxies.New(serviceType, routingInterceptor);
        return routingProxy;
    }
}
