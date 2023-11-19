using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Rpc.Caching;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcOutboundContext(List<RpcHeader>? headers = null)
{
    [ThreadStatic] private static RpcOutboundContext? _current;

#pragma warning disable CA1721
    public static RpcOutboundContext? Current => _current;
#pragma warning restore CA1721

    public List<RpcHeader>? Headers = headers;
    public RpcMethodDef? MethodDef;
    public ArgumentList? Arguments;
    public CancellationToken CancellationToken;
    public byte CallTypeId;
    public RpcOutboundCall? Call { get; private set; }
    public RpcPeer? Peer;
    public long RelatedCallId;
    public RpcCacheInfoCapture? CacheInfoCapture;

    public static RpcOutboundContext GetCurrent()
        => Current ?? throw Errors.NoCurrentRpcOutboundContext();

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

    public Scope Activate()
        => new(this, _current);

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
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
            hub.OutboundMiddlewares.NullIfEmpty()?.PrepareCall(this);
        return Call;
    }

    // Nested types

    public readonly struct Scope : IDisposable
    {
        private readonly RpcOutboundContext? _oldContext;

        public readonly RpcOutboundContext Context;

        internal Scope(RpcOutboundContext context, RpcOutboundContext? oldContext)
        {
            Context = context;
            _oldContext = oldContext;
            if (Context != _oldContext)
                _current = context;
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
