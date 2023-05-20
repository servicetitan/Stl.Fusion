using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcOutboundContext
{
    private static readonly AsyncLocal<RpcOutboundContext?> CurrentLocal = new();

    public static RpcOutboundContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcOutboundContext();

    public static Scope NewOrActive()
    {
        var oldContext = CurrentLocal.Value;
        var context = oldContext ?? new RpcOutboundContext();
        return new Scope(context, oldContext);
    }

    public List<RpcHeader> Headers { get; set; } = new();
    public RpcMethodDef? MethodDef { get; private set; }
    public ArgumentList? Arguments { get; private set; }
    public CancellationToken CancellationToken { get; private set; } = default;
    public IRpcOutboundCall? Call { get; private set; }
    public RpcPeer? Peer { get; set; }
    public long RelatedCallId { get; set; }

    public Scope Activate()
        => new(this);

    public async Task SendCall(RpcMethodDef methodDef, ArgumentList arguments)
    {
        if (Call != null)
            throw Stl.Internal.Errors.AlreadyInvoked(nameof(SendCall));

        // MethodDef, Arguments, CancellationToken
        MethodDef = methodDef;
        Arguments = arguments;
        var ctIndex = methodDef.CancellationTokenIndex;
        CancellationToken = ctIndex >= 0 ? arguments.GetCancellationToken(ctIndex) : default;

        // Peer
        Peer ??= MethodDef.Hub.PeerResolver.Invoke(this);

        // Call
        var call = Call = methodDef.CallFactory.CreateOutbound(this);
        await call.Send().ConfigureAwait(false);

        if (!MethodDef.NoWait) {
            var ctr = CancellationToken.Register(state => {
                var context = (RpcOutboundContext)state;
                var peer1 = context.Peer!;
                var call1 = context.Call!;
                var systemCallSender = peer1.Hub.SystemCallSender;
                systemCallSender.Cancel(peer1, call1.Id);
            }, this, false);
            _ = call.ResultTask.ContinueWith(
                _ => ctr.Dispose(),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    // Nested types

    public readonly struct Scope : IDisposable
    {
        private readonly RpcOutboundContext? _oldContext;

        public readonly RpcOutboundContext Context;

        internal Scope(RpcOutboundContext context)
        {
            Context = context;
            _oldContext = CurrentLocal.Value;
            TryActivate(context);
        }

        internal Scope(RpcOutboundContext context, RpcOutboundContext? oldContext)
        {
            Context = context;
            _oldContext = oldContext;
            TryActivate(context);
        }

        public void Dispose()
            => TryActivate(_oldContext);

        private void TryActivate(RpcOutboundContext? context)
        {
            if (Context != _oldContext)
                CurrentLocal.Value = context;
        }
    }
}
