using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

    public static readonly RpcInboundContextFactory DefaultFactory =
        static (peer, message, cancellationToken) => new RpcInboundContext(peer, message, cancellationToken);
    public static RpcInboundContext Current => CurrentLocal.Value
        ?? throw Errors.NoCurrentRpcInboundContext();

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public CancellationToken CancellationToken { get; }
    public List<RpcHeader> Headers => Message.Headers;
    public Type CallType { get; set; } = typeof(RpcInboundCall<>);
    public RpcInboundCall Call { get; private set; }

    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
        Call = RpcInboundCall.New(this, GetMethodDef());
    }

    public Scope Activate()
        => new(this);

    // Private methods

    private RpcMethodDef GetMethodDef()
    {
        var serviceDef = Peer.Hub.ServiceRegistry[Message.Service];
        if (!serviceDef.IsSystem && !Peer.LocalServiceFilter.Invoke(serviceDef))
            throw Errors.ServiceIsNotWhiteListed(serviceDef);

        return serviceDef[Message.Method];
    }

    // Nested types

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
