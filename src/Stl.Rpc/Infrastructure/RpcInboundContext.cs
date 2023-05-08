using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

    public static RpcInboundContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcInboundContext();

    private RpcMethodDef? _methodDef;

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public List<RpcHeader> Headers => Message.Headers;
    public RpcMethodDef MethodDef => _methodDef ??= GetMethodDef();
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

    // Private methods

    private RpcMethodDef GetMethodDef()
    {
        var serviceDef = Peer.Hub.ServiceRegistry[Message.Service];
        if (!serviceDef.IsSystem && !Peer.LocalServiceFilter.Invoke(serviceDef))
            throw Errors.ServiceIsNotWhiteListed(serviceDef);

        return serviceDef[Message.Method];
    }
}
