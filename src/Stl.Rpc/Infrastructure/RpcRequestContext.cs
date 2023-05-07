using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcRequestContext
{
    private static readonly AsyncLocal<RpcRequestContext?> CurrentLocal = new();

    public static RpcRequestContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcRequestContext();

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public RpcBoundRequest? BoundRequest { get; set; }
    public CancellationToken CancellationToken { get; }

    public RpcRequestContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
    }

    public ClosedDisposable<RpcRequestContext?> Activate()
    {
        var oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent, static oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }
}
