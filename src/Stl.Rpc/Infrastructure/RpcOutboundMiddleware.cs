namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundMiddleware(IServiceProvider services)
    : RpcMiddleware(services)
{
    public virtual void PrepareCall(RpcOutboundContext context) { }
}
