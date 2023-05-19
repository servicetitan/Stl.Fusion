using Stl.Interception;
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
    public CancellationToken CancellationToken { get; }
    public RpcMethodDef MethodDef => _methodDef ??= GetMethodDef();
    public ArgumentList? Arguments { get; set; }
    public IRpcInboundCall? Call { get; private set; }

    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
    }

    public Scope Activate()
        => new(this);

    public Task ProcessCall()
    {
        if (Call != null)
            throw Stl.Internal.Errors.AlreadyInvoked(nameof(ProcessCall));

        Call = MethodDef.CallFactory.CreateInbound(this);
        return Call.Process();
    }

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
