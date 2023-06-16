using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

    public static RpcInboundContext? Current => CurrentLocal.Value;

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public CancellationToken CancellationToken { get; }
    public RpcInboundCall Call { get; protected init; }

    public static RpcInboundContext GetCurrent()
        => CurrentLocal.Value ?? throw Errors.NoCurrentRpcInboundContext();

    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
        : this(peer, message, cancellationToken, true)
    { }

    protected RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken, bool initializeCall)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
        if (initializeCall) {
            Call = RpcInboundCall.New(message.CallTypeId, this, GetMethodDef());
        }
        else
            Call = null!;
    }

    public Scope Activate()
        => new(this);

    // Nested types

    private RpcMethodDef? GetMethodDef()
    {
        var serviceDef = Peer.Hub.ServiceRegistry.Get(Message.Service);
        if (serviceDef == null)
            return null;

        if (!serviceDef.IsSystem && !Peer.LocalServiceFilter.Invoke(serviceDef))
            return null;

        return serviceDef.Get(Message.Method);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly RpcInboundContext? _oldContext;

        public readonly RpcInboundContext Context;

        internal Scope(RpcInboundContext context)
        {
            Context = context;
            _oldContext = CurrentLocal.Value;
            TryActivate(context);
        }

        internal Scope(RpcInboundContext context, RpcInboundContext? oldContext)
        {
            Context = context;
            _oldContext = oldContext;
            TryActivate(context);
        }

        public void Dispose()
            => TryActivate(_oldContext);

        private void TryActivate(RpcInboundContext? context)
        {
            if (Context != _oldContext)
                CurrentLocal.Value = context;
        }
    }
}
