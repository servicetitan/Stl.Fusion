using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcCallTracker<TRpcCall> : IEnumerable<TRpcCall>
    where TRpcCall : RpcCall
{
    private RpcPeer _peer = null!;
    protected readonly ConcurrentDictionary<long, TRpcCall> Calls = new();

    public RpcPeer Peer {
        get => _peer;
        protected set {
            if (_peer != null)
                throw Errors.AlreadyInitialized(nameof(Peer));
            _peer = value;
        }
    }

    public int Count => Calls.Count;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<TRpcCall> GetEnumerator() => Calls.Values.GetEnumerator();

    public virtual void Initialize(RpcPeer peer)
        => Peer = peer;

    public TRpcCall? Get(long callId)
        => Calls.GetValueOrDefault(callId);

    public bool Unregister(TRpcCall call)
        // NoWait should always return true here!
        => call.NoWait || Calls.TryRemove(call.Id, call);
}

public sealed class RpcInboundCallTracker : RpcCallTracker<RpcInboundCall>
{
    public RpcInboundCall GetOrRegister(RpcInboundCall call)
    {
        if (call.NoWait || Calls.TryAdd(call.Id, call))
            return call;

        // We could use this call earlier, but it's more expensive,
        // and we should rarely land here, so we do this separately
        return Calls.GetOrAdd(call.Id, static (_, call1) => call1, call);
    }

    public void Clear()
        => Calls.Clear();
}

public sealed class RpcOutboundCallTracker : RpcCallTracker<RpcOutboundCall>
{
    public static TimeSpan AbortCheckPeriod { get; set; } = TimeSpan.FromSeconds(1);

    private long _lastId;

    public void Register(RpcOutboundCall call)
    {
        if (call.NoWait || call.Id != 0)
            return;

        while (true) {
            call.Id = Interlocked.Increment(ref _lastId);
            if (Calls.TryAdd(call.Id, call))
                return;
        }
    }

    public async Task Abort(Exception error)
    {
        var abortedCallIds = new HashSet<long>();
        for (int i = 0;; i++) {
            var abortedCallCountBefore = abortedCallIds.Count;
            foreach (var call in this) {
                if (abortedCallIds.Add(call.Id))
                    call.SetError(error, null, false);
            }
            if (i >= 2 && abortedCallCountBefore == abortedCallIds.Count)
                break;

            await Task.Delay(AbortCheckPeriod).ConfigureAwait(false);
        }
    }
}
