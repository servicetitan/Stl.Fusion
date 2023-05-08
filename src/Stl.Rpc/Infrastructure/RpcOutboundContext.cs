using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcOutboundContext
{
    private static readonly AsyncLocal<RpcOutboundContext?> CurrentLocal = new();

    public static RpcOutboundContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcOutboundContext();

    public RpcCall Call { get; set; }
    public RpcPeer? Peer { get; }
    public RpcMessage? Message { get; }
    public List<RpcHeader> Headers { get; } = new();
    public CancellationToken CancellationToken { get; }

    public RpcOutboundContext(RpcCall call, CancellationToken cancellationToken)
    {
        Call = call;
        CancellationToken = cancellationToken;
    }

    public ClosedDisposable<RpcOutboundContext?> Activate()
    {
        var oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent, static oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }
}
