namespace Stl.Rpc.Infrastructure;

public abstract class RpcInboundMiddleware(IServiceProvider services)
    : RpcMiddleware(services)
{
    public virtual void BeforeCall(RpcInboundCall call) { }
    public virtual void OnResultTaskReady(RpcInboundCall call) { }
}
