using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcOutboundComputeCall<T> : RpcOutboundCall<T>
{
    public RpcOutboundComputeCall(RpcOutboundContext context)
        : base(context)
        => context.Headers.Add(RpcFusionHeaders.Call);
}
