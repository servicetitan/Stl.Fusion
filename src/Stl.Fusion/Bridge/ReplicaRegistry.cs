using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
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
        (IReplica Replica, bool IsNew) GetOrAdd(Symbol publicationId, Func<IReplica> replicaFactory);
        bool Remove(IReplica replica);
    }

    public sealed class ReplicaRegistry : IReplicaRegistry, IDisposable
    {
        public static readonly IReplicaRegistry Default = new ReplicaRegistry();

        public sealed class Options
        {
            public int InitialCapacity { get; set; } = 509;
            public int ConcurrencyLevel { get; set; } = HardwareInfo.ProcessorCount;
            public GCHandlePool? GCHandlePool { get; set; } = null;
        }

        private readonly ConcurrentDictionary<Symbol, GCHandle> _handles;
        private readonly StochasticCounter _opCounter;
        private readonly GCHandlePool _gcHandlePool;
        private volatile int _pruneCounterThreshold;
        private Task? _pruneTask = null;
        private object Lock => _handles; 

        public ReplicaRegistry(Options? options = null)
        {
            options ??= new Options();
            _handles = new ConcurrentDictionary<Symbol, GCHandle>(options.ConcurrencyLevel, options.InitialCapacity);
            _opCounter = new StochasticCounter();
            _gcHandlePool = options.GCHandlePool ?? new GCHandlePool(GCHandleType.Weak);
            if (_gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(options)}.{nameof(options.GCHandlePool)}.{nameof(_gcHandlePool.HandleType)}");
            UpdatePruneCounterThreshold(); 
        }

        public void Dispose() 
            => _gcHandlePool.Dispose();

        public IReplica? TryGet(Symbol publicationId)
        {
            var random = publicationId.HashCode;
            OnOperation(random);
            if (!_handles.TryGetValue(publicationId, out var handle))
                return null;
            var target = (IReplica?) handle.Target;
            if (target != null)
                return target;
            // GCHandle target == null => we have to recycle it 
            if (!_handles.TryRemove(publicationId, handle))
                // Some other thread already removed this entry
                return null;
            // The thread that succeeds in removal releases gcHandle as well
            _gcHandlePool.Release(handle, random);
            return null;
        }

        public (IReplica Replica, bool IsNew) GetOrAdd(Symbol publicationId, Func<IReplica> replicaFactory)
        {
            var random = publicationId.HashCode;
            OnOperation(random);
            var spinWait = new SpinWait();
            var newReplica = (IReplica?) null; // Just to make sure we store this ref
            while (true) {
                // ReSharper disable once HeapView.CanAvoidClosure
                var handle = _handles.GetOrAdd(publicationId, _ => {
                    newReplica = replicaFactory.Invoke();
                    return _gcHandlePool.Acquire(newReplica, random);
                });
                var target = (IReplica?) handle.Target;
                if (target != null) {
                    if (target == newReplica)
                        return (target, true);
                    (newReplica as IReplicaImpl)?.MarkDisposed();
                    return (target, false);
                }
                // GCHandle target == null => we have to recycle it 
                if (_handles.TryRemove(publicationId, handle))
                    // The thread that succeeds in removal releases gcHandle as well
                    _gcHandlePool.Release(handle, random);
                // And since we didn't manage to add the replica, let's retry
                spinWait.SpinOnce();
            }
        }

        public bool Remove(IReplica replica)
        {
            var publicationId = replica.PublicationId;
            var publisherId = replica.PublisherId;
            var random = publicationId.HashCode;
            OnOperation(random);
            if (!_handles.TryGetValue(publicationId, out var handle))
                return false;
            var target = handle.Target;
            if (target != null && !ReferenceEquals(target, replica))
                // GCHandle target is pointing to another replica
                return false;
            if (!_handles.TryRemove(publicationId, handle))
                // Some other thread already removed this entry
                return false;
            // The thread that succeeds in removal releases gcHandle as well
            _gcHandlePool.Release(handle, random);
            return true;
        }

        // Private members

        private void UpdatePruneCounterThreshold()
        {
            lock (Lock) {
                // Should be called inside Lock
                var currentThreshold = (long) _pruneCounterThreshold;
                var capacity = (long) _handles.GetCapacity();
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
            foreach (var (key, gcHandle) in _handles) {
                if (gcHandle.Target != null)
                    continue;
                if (!_handles.TryRemove(key, gcHandle))
                    continue;
                var random = key.HashCode;
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
