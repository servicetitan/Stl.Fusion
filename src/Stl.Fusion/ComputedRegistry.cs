using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Locking;
using Stl.OS;
using Stl.Time;
using Stl.Time.Internal;

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
        public static readonly IComputedRegistry Default = new ComputedRegistry();

        public sealed class Options
        {
            public int InitialCapacity { get; set; } = 7919;
            public int ConcurrencyLevel { get; set; } = HardwareInfo.ProcessorCount;
            public Func<IFunction, IAsyncLockSet<ComputedInput>>? LocksProvider { get; set; } = null;
            public GCHandlePool? GCHandlePool { get; set; } = null;
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        private readonly ConcurrentDictionary<ComputedInput, Entry> _storage;
        private readonly Func<IFunction, IAsyncLockSet<ComputedInput>> _locksProvider; 
        private readonly GCHandlePool _gcHandlePool;
        private readonly StochasticCounter _opCounter;
        private readonly IMomentClock _clock;
        private volatile int _pruneCounterThreshold;
        private Task? _pruneTask;
        private object Lock => _storage; 

        public ComputedRegistry(Options? options = null) 
        {
            options ??= new Options();
            _storage = new ConcurrentDictionary<ComputedInput, Entry>(options.ConcurrencyLevel, options.InitialCapacity);
            var locksProvider = options.LocksProvider;
            if (locksProvider == null) {
                var locks = new AsyncLockSet<ComputedInput>(ReentryMode.CheckedFail);
                locksProvider = _ => locks;
            }
            _locksProvider = locksProvider;
            _gcHandlePool = options.GCHandlePool ?? new GCHandlePool(GCHandleType.Weak);
            if (_gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(options)}.{nameof(options.GCHandlePool)}.{nameof(_gcHandlePool.HandleType)}");
            _opCounter = new StochasticCounter();
            _clock = options.Clock;
            UpdatePruneCounterThreshold(); 
        }

        public void Dispose() 
            => _gcHandlePool.Dispose();

        public IComputed? TryGet(ComputedInput key)
        {
            var random = Randomize(key.HashCode);
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
            return null;
        }

        public void Store(IComputed value)
        {
            if (!value.IsConsistent) // It could be invalidated on the way here :)
                return;
            var key = value.Input;
            var random = Randomize(key.HashCode);
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
            var random = Randomize(key.HashCode);
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
        private int Randomize(int random) 
            => random + CoarseStopwatch.RandomInt32;

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

        private void Prune()
        {
            lock (Lock) {
                if (_pruneTask == null || _pruneTask.IsCompleted)
                    _pruneTask = Task.Run(PruneInternal);
            }
        }

        private void PruneInternal()
        {
            var now = _clock.Now;
            var randomOffset = Randomize(Thread.CurrentThread.ManagedThreadId);
            foreach (var (key, entry) in _storage) {
                var handle = entry.Handle;
                if (handle.Target == null) {
                    if (_storage.TryRemove(key, entry)) {
                        var random = key.HashCode + randomOffset;
                        _gcHandlePool.Release(handle, random);
                    }
                    continue;
                }
                var computed = entry.Computed;
                if (computed == null)
                    continue;
                var expirationTime = computed.LastAccessTime + computed.Options.KeepAliveTime;
                if (expirationTime >= now)
                    continue;
                _storage.TryUpdate(key, new Entry(null, handle), entry);
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
                var capacity = (long) _storage.GetCapacity();
                var nextThreshold = (int) Math.Min(int.MaxValue >> 1, capacity << 1);
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
