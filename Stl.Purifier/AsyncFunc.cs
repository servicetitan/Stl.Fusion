using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Locking;

namespace Stl.Purifier
{
    public interface IAsyncFunc : IAsyncDisposable { }
    public interface IAsyncFunc<in TKey, TValue> : IAsyncFunc
        where TKey : notnull
    {
        ValueTask<TValue> InvokeAsync(TKey key,
            Computation? dependentComputation = null,
            CancellationToken cancellationToken = default);

        bool Invalidate(TKey key);
    }

    public abstract class AsyncFuncBase<TKey, TValue> : AsyncDisposableBase,
        IAsyncFunc<TKey, TValue>
        where TKey : notnull
    {
        protected Action<Computation> OnInvalidateHandler { get; set; }
        protected AsyncLockSet<TKey> Locks { get; } 
            = new AsyncLockSet<TKey>(ReentryMode.CheckedFail);

        protected AsyncFuncBase()
        {
            OnInvalidateHandler = c => RemoveComputation((Computation<TKey, TValue>) c);
        }

        public async ValueTask<TValue> InvokeAsync(TKey key, 
            Computation? dependentComputation = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var computationOpt = TryGetComputation(key);
            if (computationOpt.IsSome(out var computation)) {
                var valueOpt = await computation.TryGetValue().ConfigureAwait(false);
                if (valueOpt.IsSome(out var value)) {
                    dependentComputation?.AddDependency(computation);
                    return value;
                }
                computation.Invalidate();
            }

            using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            
            computationOpt = TryGetComputation(key);
            if (computationOpt.IsSome(out computation)) {
                var valueOpt = await computation.TryGetValue().ConfigureAwait(false);
                if (valueOpt.IsSome(out var value)) {
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
            var computationOpt = TryGetComputation(key);
            if (computationOpt.IsSome(out var computation))
                return computation.Invalidate();
            return false;
        }

        // Protected & private

        protected abstract Option<Computation<TKey, TValue>> TryGetComputation(TKey key);
        protected abstract void StoreComputation(Computation<TKey, TValue> computedComputation);
        protected abstract void RemoveComputation(Computation<TKey, TValue> computedComputation);
        protected abstract ValueTask<(Computation<TKey, TValue> Computation, TValue Value)> 
            ComputeAsync(TKey key, CancellationToken cancellationToken);
    }
}
