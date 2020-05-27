using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Locking;
using Stl.OS;
using Stl.Reflection;
using Stl.Time;

namespace Stl.Fusion
{
    public interface IComputedRegistry
    {
        IComputed? TryGet(ComputedInput key);
        void Store(IComputed value);
        bool Remove(IComputed value);
        IAsyncLockSet<ComputedInput> GetLocksFor(IFunction function);
    }

    public sealed class ComputedRegistry : IComputedRegistry, IDisposable
    {
        public static readonly int DefaultInitialCapacity = 7919; // Ideally, a prime number
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount;
        public static readonly IComputedRegistry Default = new ComputedRegistry();

        private readonly ConcurrentDictionary<ComputedInput, Entry> _storage;
        private readonly Func<IFunction, IAsyncLockSet<ComputedInput>> _locksProvider; 
        private readonly GCHandlePool _gcHandlePool;
        private readonly StochasticCounter _opCounter;
        private volatile int _pruneCounterThreshold;
        private Task? _pruneTask = null;
        private object Lock => _storage; 

        public ComputedRegistry() 
            : this(DefaultConcurrencyLevel, DefaultInitialCapacity) { }
        public ComputedRegistry(
            int concurrencyLevel, 
            int initialCapacity,
            Func<IFunction, IAsyncLockSet<ComputedInput>>? locksProvider = null,
            GCHandlePool? gcHandlePool = null)
        {
            if (locksProvider == null) {
                var locks = new AsyncLockSet<ComputedInput>(ReentryMode.CheckedFail);
                locksProvider = _ => locks;
            }
            gcHandlePool ??= new GCHandlePool(GCHandleType.Weak);
            if (gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(gcHandlePool)}.{nameof(_gcHandlePool.HandleType)}");

            _locksProvider = locksProvider;
            _gcHandlePool = gcHandlePool;
            _opCounter = new StochasticCounter();
            _storage = new ConcurrentDictionary<ComputedInput, Entry>(concurrencyLevel, initialCapacity);
            UpdatePruneCounterThreshold(); 
        }

        public void Dispose() 
            => _gcHandlePool.Dispose();

        public IComputed? TryGet(ComputedInput key)
        {
            var random = key.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            if (_storage.TryGetValue(key, out var entry)) {
                var value = entry.Computed;
                if (value != null) {
                    value.Touch();
                    return value;
                }

                var handle = entry.Handle;
                value = (IComputed?) handle.Target;
                if (value != null) {                        
                    value.Touch();
                    _storage.TryUpdate(key, new Entry(value, handle), entry);
                    return value;
                }
                if (_storage.TryRemove(key, entry))
                    _gcHandlePool.Release(handle, random);
            }
            // Debug.WriteLine($"Cache miss: {key}");
            return null;
        }

        public void Store(IComputed value)
        {
            if (!value.IsConsistent) // It could be invalidated on the way here :)
                return;
            var key = value.Input;
            var random = key.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            _storage.AddOrUpdate(
                key, 
                (key1, s) => new Entry(s.Value, s.This._gcHandlePool.Acquire(s.Value, s.Random)), 
                (key1, entry, s) => {
                    // Not sure how we can reach this point,
                    // but if we are here somehow, let's reuse the handle.
                    var handle = entry.Handle;
                    handle.Target = s.Value;
                    return new Entry(s.Value, handle);
                },
                (This: this, Value: value, Random: random));
        }

        public bool Remove(IComputed value)
        {
            var key = value.Input;
            var random = key.HashCode + IntMoment.Clock.EpochOffsetUnits;
            OnOperation(random);
            if (!_storage.TryGetValue(key, out var entry))
                return false;
            var handle = entry.Handle;
            var target = handle.Target;
            if (target != null && !ReferenceEquals(target, value))
                return false;
            // gcHandle.Target == null (is gone, i.e. to be pruned)
            // or pointing to the right computation object
            if (!_storage.TryRemove(key, entry))
                // If another thread removed the entry, it also released the handle
                return false;
            _gcHandlePool.Release(handle, random);
            return true;
        }

        public IAsyncLockSet<ComputedInput> GetLocksFor(IFunction function) 
            => _locksProvider.Invoke(function);

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
                if (_pruneTask != null)
                    _pruneTask = Task.Run(Prune);
            }
        }

        private void Prune()
        {
            var now = IntMoment.Clock.EpochOffsetUnits;
            foreach (var (key, entry) in _storage) {
                var handle = entry.Handle;
                if (handle.Target == null) {
                    if (_storage.TryRemove(key, entry)) {
                        var random = key.HashCode + now;
                        _gcHandlePool.Release(handle, random);
                    }
                    continue;
                }
                var computed = entry.Computed;
                if (computed == null)
                    continue;
                var expirationTime = computed.LastAccessTime.EpochOffsetUnits + computed.KeepAliveTime;
                if (expirationTime >= now)
                    continue;
                _storage.TryUpdate(key, new Entry(null, handle), entry);
            }

            lock (Lock) {
                UpdatePruneCounterThreshold();
                _opCounter.ApproximateValue = 0;
                _pruneTask = null;
            }
        }

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

        private readonly struct Entry : IEquatable<Entry>
        {
            public readonly IComputed? Computed;
            public readonly GCHandle Handle;

            public Entry(IComputed? computed, GCHandle handle)
            {
                Computed = computed;
                Handle = handle;
            }

            public bool Equals(Entry other) 
                => Computed == other.Computed && Handle == other.Handle;
            public override bool Equals(object? obj) 
                => obj is Entry other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Computed, Handle);
            public static bool operator ==(Entry left, Entry right) => left.Equals(right);
            public static bool operator !=(Entry left, Entry right) => !left.Equals(right);
        }
    }
}
