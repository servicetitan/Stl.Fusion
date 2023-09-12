using Stl.Concurrency;
using Stl.Internal;
using Stl.OS;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcObjectTracker
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
}

public class RpcRemoteObjectTracker : RpcObjectTracker, IEnumerable<IRpcObject>
{
    public static TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(15);
    public static GCHandlePool GCHandlePool { get; set; } = new(new GCHandlePool.Options() {
        Capacity = HardwareInfo.GetProcessorCountPo2Factor(16),
    });

    private readonly ConcurrentDictionary<long, GCHandle> _objects = new();

    public override int Count => _objects.Count;

    public IRpcObject? Get(long id)
        => _objects.TryGetValue(id, out var handle) && handle.Target is IRpcObject obj
            ? obj
            : null;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IRpcObject> GetEnumerator()
    {
        foreach (var (_, handle) in _objects) {
            if (handle.Target is IRpcObject obj)
                yield return obj;
        }
    }

    public void Register(IRpcObject obj)
    {
        var id = obj.Id;
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(obj));

        obj.RequireKind(RpcObjectKind.Remote);
        var handle = GCHandlePool.Acquire(obj, obj.GetHashCode());
        _objects[id] = handle;
    }

    public bool Unregister(IRpcObject obj)
    {
        if (!(_objects.TryGetValue(obj.Id, out var handle)
            && handle.Target is IRpcObject existingObj
            && ReferenceEquals(obj, existingObj)))
            return false;

        if (!_objects.TryRemove(obj.Id, handle))
            return false;

        GCHandlePool.Release(handle);
        return true;
    }

    public async Task Maintain(CancellationToken cancellationToken)
    {
        try {
            foreach (var (_, handle) in _objects)
                if (handle.Target is IRpcObject obj)
                    await obj.OnReconnected(cancellationToken).ConfigureAwait(false);

            var hub = Peer.Hub;
            var clock = hub.Clock;
            var systemCallSender = hub.SystemCallSender;
            while (true) {
                await clock.Delay(KeepAlivePeriod, cancellationToken).ConfigureAwait(false);
                var objectIds = GetAliveObjectIdsAndReleaseDeadHandles();
                await systemCallSender.KeepAlive(Peer, objectIds).ConfigureAwait(false);
            }
        }
        catch {
            // Intended
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void MissingObjects(long[] objectIds)
    {
        foreach (var objectId in objectIds)
            Get(objectId)?.OnMissing();
    }

    // Private methods

    private long[] GetAliveObjectIdsAndReleaseDeadHandles()
    {
        var buffer = ArrayBuffer<long>.Lease(false);
        var purgeBuffer = ArrayBuffer<(long, GCHandle)>.Lease(false);
        try {
            foreach (var (id, handle) in _objects) {
                if (handle.Target is IRpcObject)
                    buffer.Add(id);
                else
                    purgeBuffer.Add((id, handle));
            }

            foreach (var (id, handle) in purgeBuffer)
                if (_objects.TryRemove(id, handle))
                    GCHandlePool.Release(handle);
            return buffer.ToArray();
        }
        finally {
            purgeBuffer.Release();
            buffer.Release();
        }
    }
}

public sealed class RpcSharedObjectTracker : RpcObjectTracker, IEnumerable<IRpcSharedObject>
{
    public static TimeSpan ReleasePeriod { get; set; } = TimeSpan.FromSeconds(10);
    public static TimeSpan ReleaseTimeout { get; set; }= TimeSpan.FromSeconds(65);
    public static TimeSpan AbortCheckPeriod { get; set; } = TimeSpan.FromSeconds(1);

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
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(obj));

        obj.RequireKind(RpcObjectKind.Local);
        if (!_objects.TryAdd(id, obj))
            throw Internal.Errors.RpcObjectIsAlreadyUsed();
    }

    public bool Unregister(IRpcSharedObject obj)
        => _objects.TryRemove(obj.Id, obj);

    public async Task Maintain(CancellationToken cancellationToken)
    {
        try {
            var hub = Peer.Hub;
            var clock = hub.Clock;
            while (true) {
                await clock.Delay(ReleasePeriod, cancellationToken).ConfigureAwait(false);
                var minLastKeepAliveAt = CpuTimestamp.Now - ReleaseTimeout;
                foreach (var (_, obj) in _objects)
                    if (obj.LastKeepAliveAt < minLastKeepAliveAt && Unregister(obj))
                        TryDispose(obj);
            }
        }
        catch {
            // Intended
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void OnKeepAlive(long[] objectIds)
    {
        var buffer = MemoryBuffer<long>.Lease(false);
        try {
            foreach (var id in objectIds) {
                if (Get(id) is { } obj)
                    obj.OnKeepAlive();
                else
                    buffer.Add(id);
            }
            if (buffer.Count > 0)
                _ = Peer.Hub.SystemCallSender.MissingObjects(Peer, buffer.ToArray());
        }
        finally {
            buffer.Release();
        }
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

    public static void TryDispose(IRpcSharedObject obj)
    {
        if (obj is IAsyncDisposable ad)
            _ = ad.DisposeAsync();
        else if (obj is IDisposable d)
            d.Dispose();
    }
}
