namespace Stl.Rpc.Infrastructure;

public abstract class RpcInboundMiddleware : RpcMiddleware
{
    protected RpcInboundMiddleware(IServiceProvider services) : base(services) { }

    public abstract void BeforeCall(RpcInboundCall call);
}
