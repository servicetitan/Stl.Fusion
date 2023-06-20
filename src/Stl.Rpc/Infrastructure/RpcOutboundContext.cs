using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundContext
{
    [ThreadStatic] private static RpcOutboundContext? _current;

    public static RpcOutboundContext? Current => _current;

    public List<RpcHeader>? Headers;
    public RpcMethodDef? MethodDef;
    public ArgumentList? Arguments;
    public CancellationToken CancellationToken;
    public byte CallTypeId;
    public RpcOutboundCall? Call { get; private set; }
    public RpcPeer? Peer;
    public long RelatedCallId;

    public static Scope Use()
    {
        var oldContext = _current;
        var context = oldContext ?? new RpcOutboundContext();
        return new Scope(context, oldContext);
    }

    public static Scope Use(byte callTypeId)
    {
        var oldContext = _current;
        var context = oldContext ?? new RpcOutboundContext();
        context.CallTypeId = callTypeId;
        return new Scope(context, oldContext);
    }

    public RpcOutboundContext(List<RpcHeader>? headers = null)
        => Headers = headers;

    public RpcOutboundCall? PrepareCall(RpcMethodDef methodDef, ArgumentList arguments)
    {
        if (MethodDef != null)
            throw Stl.Internal.Errors.AlreadyInvoked(nameof(PrepareCall));

        // MethodDef, Arguments, CancellationToken
        MethodDef = methodDef;
        Arguments = arguments;
        var ctIndex = methodDef.CancellationTokenIndex;
        CancellationToken = ctIndex >= 0 ? arguments.GetCancellationToken(ctIndex) : default;

        // Peer
        var hub = MethodDef.Hub;
        Peer ??= hub.CallRouter.Invoke(methodDef, arguments);
        if (Peer == null)
            return null;

        // Call
        Call = RpcOutboundCall.New(this);
        if (!Call.NoWait)
            hub.OutboundMiddlewares.PrepareCall(this);
        return Call;
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
