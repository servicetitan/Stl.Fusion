using Stl.Internal;

#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcStreamTracker<TStream> : IEnumerable<TStream>
{
    private RpcPeer _peer = null!;
    protected readonly ConcurrentDictionary<long, TStream> Streams = new();

    public RpcPeer Peer {
        get => _peer;
        protected set {
            if (_peer != null)
                throw Errors.AlreadyInitialized(nameof(Peer));
            _peer = value;
        }
    }

    public int Count => Streams.Count;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<TStream> GetEnumerator() => Streams.Values.GetEnumerator();

    public virtual void Initialize(RpcPeer peer)
        => Peer = peer;

    public TStream? Get(long id)
        => Streams.GetValueOrDefault(id);

    public bool Unregister(long id, TStream stream)
        => Streams.TryRemove(id, stream);
}

public class RpcIncomingStreamTracker : RpcStreamTracker<RpcStream>
{
    public void Register(RpcStream stream)
    {
        var id = stream.Id;
        if (stream.Kind != RpcStreamKind.Incoming || id == 0)
            throw new ArgumentOutOfRangeException(nameof(stream));

        if (!Streams.TryAdd(id, stream))
            throw Internal.Errors.RpcStreamIsAlreadyUsed();
    }
}

public class RpcOutgoingStreamTracker : RpcStreamTracker<RpcStreamSender>
{
    public static readonly TimeSpan AbortCheckPeriod = TimeSpan.FromSeconds(1);

    private long _lastId;

    public long Register(RpcStream stream)
    {
        var id = Interlocked.Increment(ref _lastId);
        var sender = new RpcStreamSender(id, stream);
        Streams.TryAdd(id, sender);
        return id;
    }

    public async Task<int> Abort(Exception error)
    {
        var abortedStreamIds = new HashSet<long>();
        for (int i = 0;; i++) {
            var abortedStreamCountBefore = abortedStreamIds.Count;
            foreach (var call in this) {
                if (abortedStreamIds.Add(call.Id))
                    _ = call.DisposeAsync();
            }
            if (i >= 2 && abortedStreamCountBefore == abortedStreamIds.Count)
                break;

            await Task.Delay(AbortCheckPeriod).ConfigureAwait(false);
        }
        return abortedStreamIds.Count;
    }
}
