using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcObjectTrackerBase : IEnumerable<IRpcObject>
{
    private RpcPeer _peer = null!;

    public RpcPeer Peer {
        get => _peer;
        protected set {
            if (_peer != null)
                throw Errors.AlreadyInitialized(nameof(Peer));
            _peer = value;
        }
    }

    public abstract int Count { get; }

    public virtual void Initialize(RpcPeer peer)
        => Peer = peer;

    public abstract IRpcObject? Get(long id);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public abstract IEnumerator<IRpcObject> GetEnumerator();
}

public class RpcRemoteObjectTracker : RpcObjectTrackerBase
{
    private readonly ConcurrentDictionary<long, WeakReference<IRpcObject>> _weakRefs = new();

    public override int Count => _weakRefs.Count;

    public override IRpcObject? Get(long id)
        => _weakRefs.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out var obj)
            ? obj
            : null;

    public override IEnumerator<IRpcObject> GetEnumerator()
    {
        foreach (var (_, weakRef) in _weakRefs) {
            if (weakRef.TryGetTarget(out var obj))
                yield return obj;
        }
    }

    public void Register(IRpcObject obj)
    {
        var id = obj.Id;
        if (id == 0)
            throw new ArgumentOutOfRangeException(nameof(obj));

        obj.RequireKind(RpcObjectKind.Remote);
        _weakRefs[id] = new WeakReference<IRpcObject>(obj);
    }

    public bool Unregister(IRpcObject obj)
        => _weakRefs.TryGetValue(obj.Id, out var weakRef)
            && weakRef.TryGetTarget(out var existingObj)
            && ReferenceEquals(obj, existingObj)
            && _weakRefs.TryRemove(obj.Id, weakRef);
}

public sealed class RpcLocalObjectTracker : RpcObjectTrackerBase
{
    public static readonly TimeSpan AbortCheckPeriod = TimeSpan.FromSeconds(1);

    private long _lastId;
    private readonly ConcurrentDictionary<long, IRpcObject> _objects = new();

    public override int Count => _objects.Count;

    public long NextId()
        => Interlocked.Increment(ref _lastId);

    public override IRpcObject? Get(long id)
        => _objects.GetValueOrDefault(id);

    public override IEnumerator<IRpcObject> GetEnumerator()
        => _objects.Values.GetEnumerator();

    public void Register(IRpcObject obj)
    {
        var id = obj.Id;
        if (id == 0)
            throw new ArgumentOutOfRangeException(nameof(obj));

        obj.RequireKind(RpcObjectKind.Local);
        if (!_objects.TryAdd(id, obj))
            throw Internal.Errors.RpcObjectIsAlreadyUsed();
    }

    public bool Unregister(IRpcObject obj)
        => _objects.TryRemove(obj.Id, obj);

    public async Task<int> Abort(Exception error)
    {
        var abortedIds = new HashSet<long>();
        for (int i = 0;; i++) {
            var abortedCountBefore = abortedIds.Count;
            foreach (var obj in this) {
                if (!abortedIds.Add(obj.Id))
                    continue;

                if (obj is IAsyncDisposable ad)
                    _ = ad.DisposeAsync();
                else if (obj is IDisposable d)
                    d.Dispose();
            }
            if (i >= 2 && abortedCountBefore == abortedIds.Count)
                break;

            await Task.Delay(AbortCheckPeriod).ConfigureAwait(false);
        }
        return abortedIds.Count;
    }
}
