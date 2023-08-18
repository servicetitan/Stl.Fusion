namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundMiddleware : RpcMiddleware
{
    protected RpcOutboundMiddleware(IServiceProvider services) : base(services) { }

    public abstract void PrepareCall(RpcOutboundContext context);
}
