namespace Stl.Rpc.Infrastructure;

public sealed class RpcInboundMiddlewares(IServiceProvider services)
    : RpcMiddlewares<RpcInboundMiddleware>(services)
{
    public void BeforeCall(RpcInboundCall call)
    {
        foreach (var m in Instances)
            m.BeforeCall(call);
    }
}
