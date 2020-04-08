using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Locking;

namespace Stl.Purifier
{
    public interface IFunction : IAsyncDisposable
    {
        ValueTask<IComputed> InvokeAsync(object key, 
            IComputation? dependentComputation = null,
            CancellationToken cancellationToken = default);

        bool Invalidate(object key);
    }
    public interface IFunction<TKey, TValue> : IFunction
        where TKey : notnull
    {
        ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key,
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

        async ValueTask<IComputed> IFunction.InvokeAsync(object key, 
            IComputation? dependentComputation = null,
            CancellationToken cancellationToken = default) 
            => await InvokeAsync((TKey) key, dependentComputation, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key, 
            IComputation? dependentComputation = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out var computation))
                return computation;

            using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            
            maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out computation))
                return computation;

            computation = await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            computation.Invalidated += OnInvalidateHandler;
            dependentComputation?.AddDependency(computation);
            StoreComputation(computation);
            return computation;
        }

        bool IFunction.Invalidate(object key) 
            => Invalidate((TKey) key);

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
        protected abstract ValueTask<IComputation<TKey, TValue>> ComputeAsync(TKey key, CancellationToken cancellationToken);
    }
}
