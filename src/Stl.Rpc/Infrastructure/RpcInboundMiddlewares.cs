namespace Stl.Rpc.Infrastructure;

public sealed class RpcInboundMiddlewares(IServiceProvider services)
    : RpcMiddlewares<RpcInboundMiddleware>(services)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RpcInboundMiddlewares? NullIfEmpty()
        => HasInstances ? this : null;

    public void BeforeCall(RpcInboundCall call)
    {
        foreach (var m in Instances)
            m.BeforeCall(call);
    }

    public void OnResultTaskReady(RpcInboundCall call)
    {
        foreach (var m in Instances)
            m.OnResultTaskReady(call);
    }
}
