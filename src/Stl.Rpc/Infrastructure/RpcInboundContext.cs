using System.Diagnostics.CodeAnalysis;
using Stl.Rpc.Internal;
using Errors = Stl.Rpc.Internal.Errors;

namespace Stl.Rpc.Infrastructure;

public class RpcInboundContext
{
    private static readonly AsyncLocal<RpcInboundContext?> CurrentLocal = new();

#pragma warning disable CA1721
    public static RpcInboundContext? Current => CurrentLocal.Value;
#pragma warning restore CA1721

    public readonly RpcPeer Peer;
    public readonly RpcMessage Message;
    public readonly CancellationToken CancellationToken;
    public RpcInboundCall Call { get; protected init; }

    public static RpcInboundContext GetCurrent()
        => CurrentLocal.Value ?? throw Errors.NoCurrentRpcInboundContext();

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
    public RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken)
        : this(peer, message, cancellationToken, true)
    { }

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
#pragma warning disable CA1068
    protected RpcInboundContext(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken, bool initializeCall)
#pragma warning restore CA1068
    {
        Peer = peer;
        Message = message;
        CancellationToken = cancellationToken;
        Call = initializeCall ? RpcInboundCall.New(message.CallTypeId, this, GetMethodDef()) : null!;
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
