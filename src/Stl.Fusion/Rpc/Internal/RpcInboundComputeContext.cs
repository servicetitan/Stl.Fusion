using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcInboundComputeContext : RpcInboundContext
{
    public static readonly RpcInboundContextFactory DefaultFactory = NewFactory(RpcInboundContext.DefaultFactory);

    public static RpcInboundContextFactory NewFactory(RpcInboundContextFactory nextFactory)
        => (peer, message) => message.Headers.Contains(RpcFusionHeaders.Call)
            ? new RpcInboundComputeContext(peer, message)
            : nextFactory.Invoke(peer, message);

    public RpcInboundComputeContext(RpcPeer peer, RpcMessage message)
        : base(peer, message)
        => CallType = typeof(RpcInboundComputeCall<>);
}
