using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcInboundComputeContext : RpcInboundContext
{
    public static new readonly RpcInboundContextFactory DefaultFactory = NewFactory(RpcInboundContext.DefaultFactory);

    public static RpcInboundContextFactory NewFactory(RpcInboundContextFactory nextFactory)
        => (peer, message, cancellationToken) => message.Headers.Contains(FusionRpcHeaders.ComputeMethod)
            ? new RpcInboundComputeContext(peer, message, cancellationToken)
            : nextFactory.Invoke(peer, message, cancellationToken);

    public RpcInboundComputeContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
        : base(peer, message, cancellationToken)
        => CallType = typeof(RpcInboundComputeCall<>);
}
