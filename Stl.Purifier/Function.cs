using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Locking;

namespace Stl.Purifier
{
    public class Function<TKey, TValue> : FunctionBase<TKey, TValue>
        where TKey : notnull
    {
        public Func<TKey, ValueTask<TValue>> Implementation { get; } 

        public Function(
            Func<TKey, ValueTask<TValue>> implementation,
            IComputationRegistry<(IFunction, TKey)>? computationRegistry,
            IAsyncLockSet<(IFunction, TKey)>? locks = null) 
            : base(computationRegistry, locks)
        {
            Implementation = implementation;
        }

        protected override async ValueTask<IComputation<TKey, TValue>> ComputeAsync(TKey key, CancellationToken cancellationToken)
        {
            var computation = new Computation<TKey, TValue>(this, key);
            var value = await Implementation.Invoke(key).ConfigureAwait(false);
            computation.Computed(value);
            return computation;
        }
    }
}
