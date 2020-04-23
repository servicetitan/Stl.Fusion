using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Purifier.Internal;
using Stl.Reflection;

namespace Stl.Purifier
{
    public interface IComputedRegistry<in TKey>
        where TKey : notnull
    {
        IComputed? TryGet(TKey key);
        void Store(TKey key, IComputed value);
        void Remove(TKey key, IComputed value);
    }

    public sealed class ComputedRegistry<TKey> : IComputedRegistry<TKey>, IDisposable
        where TKey : notnull
    {
        public static readonly int DefaultInitialCapacity = 509; // Ideally, a prime number

        private readonly ConcurrentDictionary<TKey, Entry> _storage;
        private readonly GCHandlePool _gcHandlePool;
        private readonly ConcurrentCounter _pruneCounter;
        private volatile int _pruneCounterThreshold;
        private Task? _pruneTask = null;
        private object Lock => _storage; 

        public ComputedRegistry() 
            : this(DefaultInitialCapacity) { }
        public ComputedRegistry(
            int initialCapacity, 
            ConcurrentCounter? pruneCounter = null, 
            GCHandlePool? gcHandlePool = null)
        {
            pruneCounter ??= new ConcurrentCounter();
            gcHandlePool ??= new GCHandlePool(GCHandleType.Weak);
            if (gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(gcHandlePool)}.{nameof(_gcHandlePool.HandleType)}");
            _gcHandlePool = gcHandlePool;
            _pruneCounter = pruneCounter;
            _storage = new ConcurrentDictionary<TKey, Entry>(pruneCounter.ConcurrencyLevel, initialCapacity);
            UpdatePruneCounterThreshold(); 
        }

        public void Dispose() 
            => _gcHandlePool.Dispose();

        public IComputed? TryGet(TKey key)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (_storage.TryGetValue(key, out var entry)) {
                var value = entry.Computed;
                if (value != null) {
                    value.Touch();
                    return value;
                }
                value = (IComputed?) entry.Handle.Target;
                if (value != null) {                        
                    value.Touch();
                    _storage.TryUpdate(key, new Entry(value, entry.Handle), entry);
                    return value;
                }
                if (_storage.TryRemove(key, entry))
                    _gcHandlePool.Release(entry.Handle, keyHash);
            }
            // Debug.WriteLine($"Cache miss: {key}");
            return null;
        }

        public void Store(TKey key, IComputed value)
        {
            if (!value.IsValid) // It could be invalidated on the way here :)
                return;
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            _storage.AddOrUpdate(
                key, 
                (key1, s) => new Entry(s.Value, s.This._gcHandlePool.Acquire(s.Value, s.KeyHash)), 
                (key1, entry, s) => {
                    // Not sure how we can reach this point,
                    // but if we are here somehow, let's reuse the handle.
                    var handle = entry.Handle;
                    handle.Target = s.Value;
                    return new Entry(s.Value, handle);
                },
                (This: this, Value: value, KeyHash: keyHash));
        }

        public void Remove(TKey key, IComputed value)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (!_storage.TryGetValue(key, out var entry))
                return;
            var target = entry.Handle.Target;
            if (target == null || ReferenceEquals(target, value)) {
                // gcHandle.Target == null (is gone, i.e. to be pruned)
                // or pointing to the right computation object
                if (_storage.TryRemove(key, entry))
                    _gcHandlePool.Release(entry.Handle, keyHash);
            }
        }

        public void MaybePrune(int random)
        {
            if (!_pruneCounter.Increment(random, out var pruneCounterValue))
                return;
            if (pruneCounterValue > _pruneCounterThreshold) lock (Lock) {
                // Double check locking
                if (_pruneCounter.ApproximateValue <= _pruneCounterThreshold)
                    return;
                _pruneCounter.ApproximateValue = 0;
                if (_pruneTask != null)
                    _pruneTask = Task.Run(Prune);
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

        private void Prune()
        {
            var now = ClickTime.Clicks;
            foreach (var (key, entry) in _storage) {
                if (!entry.Handle.IsAllocated) {
                    _storage.TryRemove(key, entry);
                    continue;
                }
                var computed = entry.Computed;
                if (computed != null && (computed.LastAccessTime + computed.KeepAliveTime) < now)
                    _storage.TryUpdate(key, new Entry(null, entry.Handle), entry);
            }

            lock (Lock) {
                UpdatePruneCounterThreshold();
                _pruneCounter.PreciseValue = 0;
                _pruneTask = null;
            }
        }

        private readonly struct Entry : IEquatable<Entry>
        {
            public readonly IComputed? Computed;
            public readonly GCHandle Handle;
            public IComputed? AnyComputed => Computed ?? (IComputed?) Handle.Target;

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
