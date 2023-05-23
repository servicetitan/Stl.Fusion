using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundContext
{
    [ThreadStatic] private static RpcOutboundContext? _current;

    public static RpcOutboundContext? Current => _current;

    public List<RpcHeader> Headers { get; set; } = new();
    public RpcMethodDef? MethodDef { get; private set; }
    public ArgumentList? Arguments { get; private set; }
    public CancellationToken CancellationToken { get; private set; } = default;
    public Type CallType { get; set; } = typeof(RpcOutboundCall<>);
    public RpcOutboundCall? Call { get; internal set; }
    public RpcPeer? Peer { get; set; }
    public long RelatedCallId { get; set; }

    public static Scope Use()
    {
        var oldContext = _current;
        var context = oldContext ?? new RpcOutboundContext();
        return new Scope(context, oldContext);
    }

    public RpcOutboundCall? Bind(RpcMethodDef methodDef, ArgumentList arguments)
    {
        if (MethodDef != null)
            throw Stl.Internal.Errors.AlreadyInvoked(nameof(Bind));

        // MethodDef, Arguments, CancellationToken
        MethodDef = methodDef;
        Arguments = arguments;
        var ctIndex = methodDef.CancellationTokenIndex;
        CancellationToken = ctIndex >= 0 ? arguments.GetCancellationToken(ctIndex) : default;

        // Peer
        Peer ??= MethodDef.Hub.PeerResolver.Invoke(methodDef, arguments);
        if (Peer == null)
            return null;

        // Call
        return Call = RpcOutboundCall.New(this);
    }

    // Nested types

    public readonly struct Scope : IDisposable
    {
        private readonly RpcOutboundContext? _oldContext;

        public readonly RpcOutboundContext Context;

        internal Scope(RpcOutboundContext context, RpcOutboundContext? oldContext)
        {
            _oldContext = oldContext;
            _current = Context = context;
        }

        public void Dispose()
        {
            if (Context != _current)
                throw Errors.RpcOutboundContextChanged();

            if (Context != _oldContext)
                _current = _oldContext;
        }
    }
}
