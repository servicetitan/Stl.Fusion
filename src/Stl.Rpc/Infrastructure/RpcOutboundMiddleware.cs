namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundMiddleware(IServiceProvider services)
    : RpcMiddleware(services)
{
    public abstract void PrepareCall(RpcOutboundContext context);
}
