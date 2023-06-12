using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

    public static RpcInboundContext Current => CurrentLocal.Value
        ?? throw Errors.NoCurrentRpcInboundContext();

    public RpcPeer Peer { get; }
    public RpcMessage Message { get; }
    public CancellationToken CancellationToken { get; }
    public List<RpcHeader> Headers => Message.Headers;
    public RpcInboundCall Call { get; protected init; }

    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
        : this(peer, message, cancellationToken, true)
    { }

    protected RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken, bool initializeCall)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
        if (initializeCall) {
            var callTypeHeader = message.Headers.GetOrDefault(RpcSystemHeaders.CallType.Name);
            var callType = Peer.Hub.Configuration.InboundCallTypes[callTypeHeader?.Value ?? ""];
            Call = RpcInboundCall.New(callType, this, GetMethodDef());
        }
        else
            Call = null!;
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
