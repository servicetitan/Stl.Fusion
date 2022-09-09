using Stl.Concurrency;
using Stl.OS;

namespace Stl.Fusion.Bridge;

public sealed class ReplicaRegistry : IDisposable
{
    public static ReplicaRegistry Instance { get; set; } = new();

    public sealed record Options
    {
        public static int DefaultInitialCapacity { get; }
        public static int DefaultInitialConcurrency { get; }
        public static Options Default { get; }

        public int InitialCapacity { get; init; } = DefaultInitialCapacity;
        public int ConcurrencyLevel { get; init; } = DefaultInitialConcurrency;
        public GCHandlePool? GCHandlePool { get; init; } = null;

        static Options()
        {
            DefaultInitialConcurrency = HardwareInfo.GetProcessorCountPo2Factor();
            var capacity = HardwareInfo.GetProcessorCountPo2Factor(16, 16);
            var ps = ComputedRegistry.Options.CapacityPrimeSieve;
            while (!ps.IsPrime(capacity))
                capacity--;
            DefaultInitialCapacity = capacity;
            Default = new();
        }
    }

    private readonly ConcurrentDictionary<PublicationRef, GCHandle> _handles;
    private readonly StochasticCounter _opCounter;
    private readonly GCHandlePool _gcHandlePool;
    private volatile int _pruneCounterThreshold;
    private Task? _pruneTask;
    private object Lock => _handles;

    public ReplicaRegistry() : this(Options.Default) { }
    public ReplicaRegistry(Options options)
    {
        _handles = new ConcurrentDictionary<PublicationRef, GCHandle>(options.ConcurrencyLevel, options.InitialCapacity);
        _opCounter = new StochasticCounter(1);
        _gcHandlePool = options.GCHandlePool ?? new GCHandlePool(GCHandleType.Weak);
        if (_gcHandlePool.HandleType != GCHandleType.Weak)
            throw new ArgumentOutOfRangeException(
                $"{nameof(options)}.{nameof(options.GCHandlePool)}.{nameof(_gcHandlePool.HandleType)}");
        UpdatePruneCounterThreshold();
    }

    public void Dispose()
        => _gcHandlePool.Dispose();

    public Replica? Get(PublicationRef publicationRef)
    {
        var random = publicationRef.PublicationId.HashCode;
        OnOperation(random);
        if (!_handles.TryGetValue(publicationRef, out var handle))
            return null;
        var target = (Replica?) handle.Target;
        if (target != null)
            return target;
        // GCHandle target == null => we have to recycle it
        if (!_handles.TryRemove(publicationRef, handle))
            // Some other thread already removed this entry
            return null;
        // The thread that succeeds in removal releases gcHandle as well
        _gcHandlePool.Release(handle, random);
        return null;
    }

    public (Replica Replica, bool IsNew) GetOrRegister(PublicationRef publicationRef, Func<Replica> replicaFactory)
    {
        var random = publicationRef.PublicationId.HashCode;
        OnOperation(random);
        var spinWait = new SpinWait();
        var newReplica = (Replica?) null; // Just to make sure we store this ref
        while (true) {
            // ReSharper disable once HeapView.CanAvoidClosure
            var handle = _handles.GetOrAdd(publicationRef, _ => {
                newReplica = replicaFactory();
                return _gcHandlePool.Acquire(newReplica, random);
            });
            var target = (Replica?) handle.Target;
            if (target != null) {
                if (target == newReplica)
                    return (target, true);
                newReplica?.DisposeTemporaryInstance();
                return (target, false);
            }
            // GCHandle target == null => we have to recycle it
            if (_handles.TryRemove(publicationRef, handle))
                // The thread that succeeds in removal releases gcHandle as well
                _gcHandlePool.Release(handle, random);
            // And since we didn't manage to add the replica, let's retry
            spinWait.SpinOnce();
        }
    }

    public bool Remove(Replica replica)
    {
        var publicationRef = replica.PublicationRef;
        var random = publicationRef.PublicationId.HashCode;
        OnOperation(random);
        if (!_handles.TryGetValue(publicationRef, out var handle))
            return false;
        var target = handle.Target;
        if (target != null && !ReferenceEquals(target, replica))
            // GCHandle target is pointing to another replica
            return false;
        if (!_handles.TryRemove(publicationRef, handle))
            // Some other thread already removed this entry
            return false;
        // The thread that succeeds in removal releases gcHandle as well
        _gcHandlePool.Release(handle, random);
        return true;
    }

    public Task Prune()
    {
        lock (Lock) {
            if (_pruneTask == null || _pruneTask.IsCompleted)
                _pruneTask = Task.Run(PruneInternal);
            return _pruneTask;
        }
    }

    // Private members

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnOperation(int random)
    {
        if (!_opCounter.Increment(random, out var opCounterValue))
            return;
        if (opCounterValue > _pruneCounterThreshold)
            TryPrune();
    }

    private void TryPrune()
    {
        lock (Lock) {
            // Double check locking
            if (_opCounter.ApproximateValue <= _pruneCounterThreshold)
                return;
            _opCounter.ApproximateValue = 0;
            Prune();
        }
    }

    private void PruneInternal()
    {
        var type = GetType();
        using var activity = type.GetActivitySource().StartActivity(type, nameof(Prune));

        foreach (var (key, gcHandle) in _handles) {
            if (gcHandle.Target == null && _handles.TryRemove(key, gcHandle))
                _gcHandlePool.Release(gcHandle, key.PublicationId.HashCode);
        }
        lock (Lock) {
            UpdatePruneCounterThreshold();
            _opCounter.ApproximateValue = 0;
        }
    }

    private void UpdatePruneCounterThreshold()
    {
        lock (Lock) {
            // Should be called inside Lock
            var capacity = (long) _handles.GetCapacity();
            var nextThreshold = (int) Math.Min(int.MaxValue >> 1, capacity);
            _pruneCounterThreshold = nextThreshold;
        }
    }
}
