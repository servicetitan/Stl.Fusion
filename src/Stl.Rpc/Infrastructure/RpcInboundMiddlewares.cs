namespace Stl.Rpc.Infrastructure;

public sealed class RpcInboundMiddlewares : RpcMiddlewares<RpcInboundMiddleware>
{
    public RpcInboundMiddlewares(IServiceProvider services) : base(services) { }

    public void BeforeCall(RpcInboundCall call)
    {
        foreach (var m in Instances)
            m.BeforeCall(call);
    }
}
