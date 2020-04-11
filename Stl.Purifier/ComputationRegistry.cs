using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Reflection;

namespace Stl.Purifier
{
    public interface IComputationRegistry<TKey>
        where TKey : notnull
    {
        Option<IComputation> TryGetComputation(TKey key);
        void StoreComputation(TKey key, IComputation computation);
        void RemoveComputation(TKey key, IComputation computation);
    }

    public class ComputationRegistry<TKey> : IComputationRegistry<TKey>, IDisposable
        where TKey : notnull
    {
        public static readonly int DefaultInitialCapacity = 509; // Ideally, a prime number
        private volatile int _pruneFactorLog2 = 2;

        protected ConcurrentDictionary<TKey, GCHandle> Computations { get; }
        protected GCHandlePool GCHandlePool { get; }
        protected ConcurrentCounter PruneCounter { get; }
        protected object Lock => Computations; 

        public int PruneFactorLog2 {
            get => _pruneFactorLog2;
            set => Interlocked.Exchange(ref _pruneFactorLog2, value);
        }

        public ComputationRegistry() 
            : this(DefaultInitialCapacity) { }
        public ComputationRegistry(
            int initialCapacity, 
            ConcurrentCounter? pruneCounter = null, 
            GCHandlePool? gcHandlePool = null)
        {
            pruneCounter ??= new ConcurrentCounter();
            gcHandlePool ??= new GCHandlePool?(GCHandleType.Weak);
            if (gcHandlePool.HandleType != GCHandleType.Weak)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(gcHandlePool)}.{nameof(GCHandlePool.HandleType)}");
            GCHandlePool = gcHandlePool;
            PruneCounter = pruneCounter;
            Computations = new ConcurrentDictionary<TKey, GCHandle>(pruneCounter.ConcurrencyLevel, initialCapacity);
        }

        protected virtual void Dispose(bool disposing) 
            => GCHandlePool.Dispose();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Option<IComputation> TryGetComputation(TKey key)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (Computations.TryGetValue(key, out var handle)) {
                var value = (IComputation?) handle.Target;
                if (!ReferenceEquals(value, null))
                    return Option.Some(value);
                if (Computations.TryRemove(key, handle))
                    GCHandlePool.Release(handle, keyHash);
            }
            return Option<IComputation>.None;
        }

        public void StoreComputation(TKey key, IComputation computation)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            Computations.AddOrUpdate(
                key, 
                (key1, s) => s.This.GCHandlePool.Acquire(s.Computation, s.KeyHash), 
                (key1, handle, s) => {
                    // Not sure how this is possible, but 
                    // let's reuse the handle if it is there somehow
                    handle.Target = s.Computation;
                    return handle;
                },
                (This: this, Computation: computation, KeyHash: keyHash));
        }

        public void RemoveComputation(TKey key, IComputation computation)
        {
            var keyHash = key.GetHashCode();
            MaybePrune(keyHash);
            if (!Computations.TryGetValue(key, out var handle))
                return;
            var value = (IComputation?) handle.Target;
            if (ReferenceEquals(value, null) || ReferenceEquals(value, computation)) {
                // gcHandle.Target == null (to be pruned)
                // or pointing to the right computation object
                if (Computations.TryRemove(key, handle))
                    GCHandlePool.Release(handle, keyHash);
            }
        }

        public void MaybePrune(int random)
        {
            if (!PruneCounter.Increment(random).IsSome(out var pruneCounterValue))
                return;
            var pruneThreshold = Computations.GetCapacity() << PruneFactorLog2;
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
            foreach (var (key, gcHandle) in Computations)
                if (!gcHandle.IsAllocated)
                    Computations.TryRemove(key, gcHandle);
        }
    }
}
