namespace Stl.Rpc.Infrastructure;

public abstract class RpcInboundMiddleware(IServiceProvider services)
    : RpcMiddleware(services)
{
    public abstract void BeforeCall(RpcInboundCall call);
}
