namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundMiddlewares : RpcMiddlewares<RpcOutboundMiddleware>
{
    public RpcOutboundMiddlewares(IServiceProvider services) : base(services) { }

    public void PrepareCall(RpcOutboundContext context)
    {
        foreach (var m in Instances)
            m.PrepareCall(context);
    }
}
