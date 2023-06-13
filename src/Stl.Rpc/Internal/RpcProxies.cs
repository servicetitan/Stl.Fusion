using Stl.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public static class RpcProxies
{
    public static object NewClientProxy(IServiceProvider services, Type serviceType, Type clientType)
    {
        var rpcHub = services.RpcHub();
        var serviceDef = rpcHub.ServiceRegistry[serviceType];

        var interceptor = services.GetRequiredService<RpcClientInterceptor>();
        interceptor.Setup(serviceDef);
        var proxy = Proxies.New(clientType, interceptor);
        return proxy;
    }

    public static object NewRoutingProxy(IServiceProvider services, Type serviceType, ServiceResolver serverResolver)
    {
        var rpcHub = services.RpcHub();
        var server = serverResolver.Resolve(services);
        var client = NewClientProxy(services, serviceType, serviceType);
        var serviceDef = rpcHub.ServiceRegistry[serviceType];

        var routingInterceptor = services.GetRequiredService<RpcRoutingInterceptor>();
        routingInterceptor.Setup(serviceDef, server, client);
        var routingProxy = Proxies.New(serviceType, routingInterceptor);
        return routingProxy;
    }
}
