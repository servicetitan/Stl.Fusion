using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.OS;
using Stl.Reflection;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Bridge
{
    public interface IReplicaRegistry
    {
        IReplica? TryGet(Symbol publicationId);
        IReplica GetOrAdd(Symbol publicationId, IReplica replica);
        bool Remove(IReplica replica);
    }

    public sealed class ReplicaRegistry : IReplicaRegistry, IDisposable
    {
        public static readonly int DefaultInitialCapacity = 509; // Ideally, a prime number
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount;
        public static readonly IReplicaRegistry Default = new ReplicaRegistry();

        private readonly ConcurrentDictionary<Symbol, GCHandle> _storage;
        private readonly GCHandlePool _gcHandlePool;
        private readonly StochasticCounter _opCounter;
        private volatile int _pruneCounterThreshold;
        private Task? _pruneTask = null;
        private object Lock => _storage; 

        public ReplicaRegistry() 
            : this(DefaultConcurrencyLevel, DefaultInitialCapacity) { }
        public ReplicaRegistry(
            int concurrencyLevel, 
            int initialCapacity,
            GCHandlePool? gcHandlePool = null)
        {
            gcHandlePool ??= new GCHandlePool(GCHandleType.Weak);
            if (gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(gcHandlePool)}.{nameof(_gcHandlePool.HandleType)}");

            _gcHandlePool = gcHandlePool;
            _opCounter = new StochasticCounter();
            _storage = new ConcurrentDictionary<Symbol, GCHandle>(concurrencyLevel, initialCapacity);
            UpdatePruneCounterThreshold(); 
        }

        public void Dispose() 
            => _gcHandlePool.Dispose();

        public IReplica? TryGet(Symbol publicationId)
        {
            var random = publicationId.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            if (!_storage.TryGetValue(publicationId, out var gcHandle))
                return null;
            var target = (IReplica?) gcHandle.Target;
            if (target != null)
                return target;
            // GCHandle target == null => we have to recycle it 
            if (!_storage.TryRemove(publicationId, gcHandle))
                // Some other thread already removed this entry
                return null;
            // The thread that succeeds in removal releases gcHandle as well
            _gcHandlePool.Release(gcHandle, random);
            return null;
        }

        public IReplica GetOrAdd(Symbol publicationId, IReplica replica)
        {
            var random = publicationId.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            var spinWait = new SpinWait();
            var gcHandle = default(GCHandle);
            while (true) {
                var oldReplica = TryGet(publicationId);
                if (oldReplica != null)
                    return oldReplica;
                if (gcHandle != default)
                    gcHandle = _gcHandlePool.Acquire(replica, random);
                if (_storage.TryAdd(publicationId, gcHandle))
                    return replica;
                spinWait.SpinOnce();
            }
        }

        public bool Remove(IReplica replica)
        {
            var publicationId = replica.PublicationId;
            var random = publicationId.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            if (!_storage.TryGetValue(publicationId, out var gcHandle))
                return false;
            var target = gcHandle.Target;
            if (target != null && !ReferenceEquals(target, replica))
                // GCHandle target is pointing to another replica
                return false;
            if (!_storage.TryRemove(publicationId, gcHandle))
                // Some other thread already removed this entry
                return false;
            // The thread that succeeds in removal releases gcHandle as well
            _gcHandlePool.Release(gcHandle, random);
            return true;
        }

        // Private members

        private void UpdatePruneCounterThreshold()
        {
            lock (Lock) {
                // Should be called inside Lock
                var currentThreshold = (long) _pruneCounterThreshold;
                var capacity = (long) _storage.GetCapacity();
                var nextThreshold = (int) Math.Min(int.MaxValue >> 1, Math.Max(capacity << 1, currentThreshold << 1));
                _pruneCounterThreshold = nextThreshold;
            }
        }

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
                if (_pruneTask != null)
                    _pruneTask = Task.Run(Prune);
            }
        }

        private void Prune()
        {
            var now = IntMoment.Clock.EpochOffsetUnits;
            foreach (var (key, gcHandle) in _storage) {
                if (gcHandle.IsAllocated)
                    continue;
                if (!_storage.TryRemove(key, gcHandle))
                    continue;
                var random = key.HashCode + now;
                _gcHandlePool.Release(gcHandle, random);
            }

            lock (Lock) {
                UpdatePruneCounterThreshold();
                _opCounter.ApproximateValue = 0;
                _pruneTask = null;
            }
        }
    }
}
