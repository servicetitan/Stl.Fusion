using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Purifier
{
    public class Function<TKey, TValue> : FunctionBase<TKey, TValue>
        where TKey : notnull
    {
        protected Func<TKey, ValueTask<TValue>> Implementation { get; }
        protected ConcurrentIdGenerator<long> TagGenerator { get; }

        public Function(
            Func<TKey, ValueTask<TValue>> implementation,
            ConcurrentIdGenerator<long> tagGenerator,
            IComputationRegistry<(IFunction, TKey)>? computationRegistry,
            IAsyncLockSet<(IFunction, TKey)>? locks = null) 
            : base(computationRegistry, locks)
        {
            Implementation = implementation;
            TagGenerator = tagGenerator;
        }

        protected override async ValueTask<IComputation<TKey, TValue>> ComputeAsync(TKey key, CancellationToken cancellationToken)
        {
            var workerId = HashCode.Combine(this, key);
            var tag = TagGenerator.Next(workerId);
            var computation = new Computation<TKey, TValue>(this, key, tag);
            var value = await Implementation.Invoke(key).ConfigureAwait(false);
            computation.Computed(value);
            return computation;
        }
    }
}
