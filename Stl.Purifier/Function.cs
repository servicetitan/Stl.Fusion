using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.OS;
using Stl.Reflection;

namespace Stl.Purifier
{
    public class Function<TKey, TValue> : FunctionBase<TKey, TValue>
        where TKey : notnull
    {
        public static readonly int DefaultCapacity = 509; // Ideally, a prime number
        public static readonly int DefaultCounterThreshold = 16;
        public static readonly int PruneFactorLog2 = 2;
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount;

        protected ConcurrentCounter PruneCounter { get; }
        protected ConcurrentDictionary<TKey, WeakReference<IComputation<TKey, TValue>>> Computations { get; }

        public Function() : this(DefaultConcurrencyLevel, DefaultCapacity) { }
        public Function(int concurrencyLevel, int capacity)
        {
            PruneCounter = new ConcurrentCounter(DefaultCounterThreshold, concurrencyLevel);
            Computations = new ConcurrentDictionary<TKey, WeakReference<IComputation<TKey, TValue>>>(concurrencyLevel, capacity);
        }

        protected override Option<IComputation<TKey, TValue>> TryGetComputation(TKey key)
        {
            MaybePrune(key.GetHashCode());
            if (Computations.TryGetValue(key, out var wr))
                if (wr.TryGetTarget(out var value))
                    return Option<IComputation<TKey, TValue>>.Some(value);
                else
                    Computations.TryRemove(key, wr);
            return Option<IComputation<TKey, TValue>>.None;
        }

        protected override void StoreComputation(IComputation<TKey, TValue> computation)
        {
            var key = computation.Key;
            MaybePrune(key.GetHashCode());
            var wr = new WeakReference<IComputation<TKey, TValue>>(computation);
            Computations.AddOrUpdate(
                key, 
                (key1, wr1) => wr1, 
                (key1, _, wr1) => wr1,
                wr);
        }

        protected override void RemoveComputation(IComputation<TKey, TValue> computation)
        {
            var key = computation.Key;
            MaybePrune(key.GetHashCode());
            if (!Computations.TryGetValue(key, out var wr))
                return;
            if (wr.TryGetTarget(out var value) && !ReferenceEquals(value, computation))
                return;
            // Weak reference is either empty (to be pruned)
            // or pointing to the right computation object
            Computations.TryRemove(key, wr);
        }

        protected override async ValueTask<IComputation<TKey, TValue>> ComputeAsync(TKey key, CancellationToken cancellationToken)
        {
            var result = new Computation<TKey, TValue>(this, key);
            // TODO: Set it to current, call compute
            return result;
        }

        protected void MaybePrune(int random)
        {
            if (!PruneCounter.Increment(random))
                return;
            var pruneThreshold = Computations.GetCapacity() << PruneFactorLog2;
            if (PruneCounter.ApproximateValue > pruneThreshold) lock (Lock) {
                // Double check locking
                if (PruneCounter.ApproximateValue > pruneThreshold) {
                    PruneCounter.ApproximateValue = 0;
                    Task.Run(Prune);
                }
            }
        }

        public void Prune()
        {
            PruneCounter.ApproximateValue = 0;
            foreach (var (key, value) in Computations)
                if (!value.TryGetTarget(out var _))
                    Computations.TryRemove(key, value);
        }
    }
}
