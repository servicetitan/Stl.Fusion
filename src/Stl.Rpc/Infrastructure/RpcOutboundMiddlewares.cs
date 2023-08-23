namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundMiddlewares(IServiceProvider services)
    : RpcMiddlewares<RpcOutboundMiddleware>(services)
{
    public void PrepareCall(RpcOutboundContext context)
    {
        foreach (var m in Instances)
            m.PrepareCall(context);
    }
}
