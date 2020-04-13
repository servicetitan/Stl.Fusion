using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
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
        private volatile int _pruneFactorLog2 = 2;

        protected ConcurrentDictionary<TKey, GCHandle> Storage { get; }
        protected GCHandlePool GCHandlePool { get; }
        protected ConcurrentCounter PruneCounter { get; }
        protected object Lock => Storage; 

        public int PruneFactorLog2 {
            get => _pruneFactorLog2;
            set => Interlocked.Exchange(ref _pruneFactorLog2, value);
        }

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
            Storage = new ConcurrentDictionary<TKey, GCHandle>(pruneCounter.ConcurrencyLevel, initialCapacity);
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
            if (Storage.TryGetValue(key, out var handle)) {
                var value = (IComputed?) handle.Target;
                if (!value.IsNull())
                    return value;
                if (Storage.TryRemove(key, handle))
                    GCHandlePool.Release(handle, keyHash);
            }
            return null;
        }

        public void Store(TKey key, IComputed value)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            Storage.AddOrUpdate(
                key, 
                (key1, s) => s.This.GCHandlePool.Acquire(s.Computation, s.KeyHash), 
                (key1, handle, s) => {
                    // Not sure how this is possible, but 
                    // let's reuse the handle if it is there somehow
                    handle.Target = s.Computation;
                    return handle;
                },
                (This: this, Computation: value, KeyHash: keyHash));
        }

        public void Remove(TKey key, IComputed value)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (!Storage.TryGetValue(key, out var handle))
                return;
            var target = (IComputed?) handle.Target;
            if (target.IsNull() || ReferenceEquals(target, value)) {
                // gcHandle.Target == null (is gone, i.e. to be pruned)
                // or pointing to the right computation object
                if (Storage.TryRemove(key, handle))
                    GCHandlePool.Release(handle, keyHash);
            }
        }

        public void MaybePrune(int random)
        {
            if (!PruneCounter.Increment(random).IsSome(out var pruneCounterValue))
                return;
            var pruneThreshold = Storage.GetCapacity() << PruneFactorLog2;
            if (pruneCounterValue > pruneThreshold) lock (Lock) {
                // Double check locking
                if (PruneCounter.ApproximateValue > pruneThreshold) {
                    PruneCounter.PreciseValue = 0;
                    Task.Run(Prune);
                }
            }
        }

        public void Prune()
        {
            PruneCounter.PreciseValue = 0;
            foreach (var (key, gcHandle) in Storage)
                if (!gcHandle.IsAllocated)
                    Storage.TryRemove(key, gcHandle);
        }
    }
}
