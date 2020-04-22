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

    public class ComputedRegistry<TKey> : IComputedRegistry<TKey>, IDisposable
        where TKey : notnull
    {
        public static readonly int DefaultInitialCapacity = 509; // Ideally, a prime number
        private volatile int _pruneCounterThreshold;

        protected ConcurrentDictionary<TKey, Entry> Storage { get; }
        protected GCHandlePool GCHandlePool { get; }
        protected ConcurrentCounter PruneCounter { get; }
        protected int PruneCounterThreshold {
            get => _pruneCounterThreshold;
            set => Interlocked.Exchange(ref _pruneCounterThreshold, value);
        }
        protected object Lock => Storage; 

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
                    $"{nameof(gcHandlePool)}.{nameof(GCHandlePool.HandleType)}");
            GCHandlePool = gcHandlePool;
            PruneCounter = pruneCounter;
            Storage = new ConcurrentDictionary<TKey, Entry>(pruneCounter.ConcurrencyLevel, initialCapacity);
            UpdatePruneCounterThreshold(Storage.GetCapacity()); 
        }

        protected virtual void Dispose(bool disposing) 
            => GCHandlePool.Dispose();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IComputed? TryGet(TKey key)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (Storage.TryGetValue(key, out var entry)) {
                var value = entry.Computed;
                if (value != null) {
                    value.Touch();
                    return value;
                }
                value = (IComputed?) entry.Handle.Target;
                if (value != null) {                        
                    value.Touch();
                    Storage.TryUpdate(key, new Entry(value, entry.Handle), entry);
                    return value;
                }
                if (Storage.TryRemove(key, entry))
                    GCHandlePool.Release(entry.Handle, keyHash);
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
            Storage.AddOrUpdate(
                key, 
                (key1, s) => new Entry(s.Value, s.This.GCHandlePool.Acquire(s.Value, s.KeyHash)), 
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
            if (!Storage.TryGetValue(key, out var entry))
                return;
            var target = entry.Handle.Target;
            if (target == null || ReferenceEquals(target, value)) {
                // gcHandle.Target == null (is gone, i.e. to be pruned)
                // or pointing to the right computation object
                if (Storage.TryRemove(key, entry))
                    GCHandlePool.Release(entry.Handle, keyHash);
            }
        }

        public void MaybePrune(int random)
        {
            if (!PruneCounter.Increment(random).IsSome(out var pruneCounterValue))
                return;
            var pruneCounterThreshold = PruneCounterThreshold;
            if (pruneCounterValue > pruneCounterThreshold) lock (Lock) {
                // Double check locking
                if (PruneCounter.ApproximateValue > pruneCounterThreshold) {
                    UpdatePruneCounterThreshold(pruneCounterThreshold);
                    PruneCounter.PreciseValue = 0;
                    Task.Run(Prune);
                }
            }
        }

        protected virtual void UpdatePruneCounterThreshold(int pruneCounterThreshold)
        {
            var nextThreshold = pruneCounterThreshold << 1;
            if (nextThreshold < pruneCounterThreshold)
                nextThreshold = int.MaxValue;
            PruneCounterThreshold = Math.Min(Storage.GetCapacity(), nextThreshold);
        }

        public void Prune()
        {
            PruneCounter.PreciseValue = 0;
            var now = ClickTime.Clicks;
            foreach (var (key, entry) in Storage) {
                if (!entry.Handle.IsAllocated) {
                    Storage.TryRemove(key, entry);
                    continue;
                }
                var computed = entry.Computed;
                if (computed != null && (computed.LastAccessTime + computed.KeepAliveTime) < now)
                    Storage.TryUpdate(key, new Entry(null, entry.Handle), entry);
            }
        }

        protected readonly struct Entry : IEquatable<Entry>
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
