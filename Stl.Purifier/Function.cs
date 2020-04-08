using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Locking;

namespace Stl.Purifier
{
    public interface IFunction : IAsyncDisposable { }
    public interface IFunction<in TKey, TValue> : IFunction
        where TKey : notnull
    {
        ValueTask<TValue> InvokeAsync(TKey key,
            IComputation? dependentComputation = null,
            CancellationToken cancellationToken = default);

        bool Invalidate(TKey key);
    }

    public abstract class FunctionBase<TKey, TValue> : AsyncDisposableBase,
        IFunction<TKey, TValue>
        where TKey : notnull
    {
        protected Action<IComputation> OnInvalidateHandler { get; set; }
        protected AsyncLockSet<TKey> Locks { get; } 
            = new AsyncLockSet<TKey>(ReentryMode.CheckedFail);

        protected FunctionBase()
        {
            OnInvalidateHandler = c => RemoveComputation((IComputation<TKey, TValue>) c);
        }

        public async ValueTask<TValue> InvokeAsync(TKey key, 
            IComputation? dependentComputation = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out var computation)) {
                var maybeValue = await computation.TryGetValue().ConfigureAwait(false);
                if (maybeValue.IsSome(out var value)) {
                    dependentComputation?.AddDependency(computation);
                    return value;
                }
                computation.Invalidate();
            }

            using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            
            maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out computation)) {
                var maybeValue = await computation.TryGetValue().ConfigureAwait(false);
                if (maybeValue.IsSome(out var value)) {
                    dependentComputation?.AddDependency(computation);
                    return value;
                }
                computation.Invalidate();
            }

            var computed = await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            computation = computed.Computation;
            computation.Invalidated += OnInvalidateHandler;
            dependentComputation?.AddDependency(computation);
            StoreComputation(computation);
            return computed.Value;
        }

        public bool Invalidate(TKey key)
        {
            var maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out var computation))
                return computation.Invalidate();
            return false;
        }

        // Protected & private

        protected abstract Option<IComputation<TKey, TValue>> TryGetComputation(TKey key);
        protected abstract void StoreComputation(IComputation<TKey, TValue> computedComputation);
        protected abstract void RemoveComputation(IComputation<TKey, TValue> computedComputation);
        protected abstract ValueTask<(IComputation<TKey, TValue> Computation, TValue Value)> 
            ComputeAsync(TKey key, CancellationToken cancellationToken);
    }
}
