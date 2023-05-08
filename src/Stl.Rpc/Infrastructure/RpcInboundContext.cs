using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

    public static RpcInboundContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcRequestContext();

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public RpcCall? Call { get; set; }
    public CancellationToken CancellationToken { get; }

    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
    }

    public ClosedDisposable<RpcInboundContext?> Activate()
    {
        var oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent, static oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }
}
