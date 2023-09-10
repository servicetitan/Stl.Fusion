using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcObjectTracker
{
    public static TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(15);

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
}

public class RpcRemoteObjectTracker : RpcObjectTracker, IEnumerable<IRpcObject>
{
    private readonly ConcurrentDictionary<long, WeakReference<IRpcObject>> _weakRefs = new();

    public override int Count => _weakRefs.Count;

    public IRpcObject? Get(long id)
        => _weakRefs.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out var obj)
            ? obj
            : null;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IRpcObject> GetEnumerator()
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

    public async Task KeepAlive(CancellationToken cancellationToken)
    {
        foreach (var (_, weakRef) in _weakRefs)
            if (weakRef.TryGetTarget(out var obj))
                await obj.OnReconnected(cancellationToken).ConfigureAwait(false);

        var hub = Peer.Hub;
        var clock = hub.Clock;
        var systemCallSender = hub.SystemCallSender;
        while (true) {
            await clock.Delay(KeepAlivePeriod, cancellationToken).ConfigureAwait(false);
            await systemCallSender.KeepAlive(Peer, GetObjectIds()).ConfigureAwait(false);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private long[] GetObjectIds()
    {
        var buffer = MemoryBuffer<long>.Lease(false);
        try {
            foreach (var (id, weakRef) in _weakRefs) {
                if (weakRef.TryGetTarget(out _))
                    buffer.Add(id);
            }
            return buffer.ToArray();
        }
        finally {
            buffer.Release();
        }
    }
}

public sealed class RpcSharedObjectTracker : RpcObjectTracker, IEnumerable<IRpcSharedObject>
{
    public static readonly TimeSpan AbortCheckPeriod = TimeSpan.FromSeconds(1);

    private long _lastId;
    private readonly ConcurrentDictionary<long, IRpcSharedObject> _objects = new();

    public override int Count => _objects.Count;

    public long NextId()
        => Interlocked.Increment(ref _lastId);

    public IRpcSharedObject? Get(long id)
        => _objects.GetValueOrDefault(id);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IRpcSharedObject> GetEnumerator()
        => _objects.Values.GetEnumerator();

    public void Register(IRpcSharedObject obj)
    {
        var id = obj.Id;
        if (id == 0)
            throw new ArgumentOutOfRangeException(nameof(obj));

        obj.RequireKind(RpcObjectKind.Local);
        if (!_objects.TryAdd(id, obj))
            throw Internal.Errors.RpcObjectIsAlreadyUsed();
    }

    public bool Unregister(IRpcSharedObject obj)
        => _objects.TryRemove(obj.Id, obj);

    public async Task KeepAlive(CancellationToken cancellationToken)
    {
        var hub = Peer.Hub;
        var clock = hub.Clock;
        var halfKeepAlivePeriod = KeepAlivePeriod / 2;
        var keepAliveTimeout = KeepAlivePeriod.Multiply(2.1);
        await clock.Delay(halfKeepAlivePeriod, cancellationToken).ConfigureAwait(false);
        while (true) {
            var minLastKeepAliveAt = CpuTimestamp.Now - keepAliveTimeout;
            foreach (var (_, obj) in _objects)
                if (obj.LastKeepAliveAt < minLastKeepAliveAt && Unregister(obj))
                    TryDispose(obj);
            await clock.Delay(KeepAlivePeriod, cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void OnKeepAlive(long[] objectIds)
    {
        foreach (var id in objectIds)
            if (Get(id) is { } obj)
                obj.OnKeepAlive();
    }

    public async Task<int> Abort(Exception error)
    {
        var abortedIds = new HashSet<long>();
        for (int i = 0;; i++) {
            var abortedCountBefore = abortedIds.Count;
            foreach (var obj in this)
                if (abortedIds.Add(obj.Id))
                    TryDispose(obj);
            if (i >= 2 && abortedCountBefore == abortedIds.Count)
                break;

            await Task.Delay(AbortCheckPeriod).ConfigureAwait(false);
        }
        return abortedIds.Count;
    }

    // Private methods

    public void TryDispose(IRpcSharedObject obj)
    {
        if (obj is IAsyncDisposable ad)
            _ = ad.DisposeAsync();
        else if (obj is IDisposable d)
            d.Dispose();
    }
}
