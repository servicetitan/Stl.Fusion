using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>
{
    public RpcInboundComputeCall(RpcInboundContext context) : base(context) { }
}
