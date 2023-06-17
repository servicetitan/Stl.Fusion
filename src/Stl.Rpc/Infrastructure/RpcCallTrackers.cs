using Stl.Internal;

#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

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

    public virtual TRpcCall Register(TRpcCall call)
    {
        if (call.NoWait || Calls.TryAdd(call.Id, call))
            return call;

        // We could use this call earlier, but it's more expensive,
        // and we should rarely land here, so we do this separately
        return Calls.GetOrAdd(call.Id, static (_, call1) => call1, call);
    }

    public virtual bool Unregister(TRpcCall call)
        // NoWait should always return true here!
        => call.NoWait || Calls.TryRemove(call.Id, call);
}

public class RpcInboundCallTracker : RpcCallTracker<RpcInboundCall>
{ }

public class RpcOutboundCallTracker : RpcCallTracker<RpcOutboundCall>
{
    private long _lastId;

    public long NextId => Interlocked.Increment(ref _lastId);
}
