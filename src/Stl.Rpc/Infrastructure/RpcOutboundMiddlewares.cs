namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundMiddlewares(IServiceProvider services)
    : RpcMiddlewares<RpcOutboundMiddleware>(services)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RpcOutboundMiddlewares? NullIfEmpty()
        => HasInstances ? this : null;

    public void PrepareCall(RpcOutboundContext context)
    {
        foreach (var m in Instances)
            m.PrepareCall(context);
    }
}
