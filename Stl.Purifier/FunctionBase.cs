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
            IComputation? dependant = null,
            CancellationToken cancellationToken = default);

        bool Invalidate(object key);
    }

    public interface IFunction<in TKey> : IFunction
        where TKey : notnull
    {
        ValueTask<IComputed> InvokeAsync(TKey key,
            IComputation? dependant = null,
            CancellationToken cancellationToken = default);

        bool Invalidate(TKey key);
    }
    
    public interface IFunction<TKey, TValue> : IFunction<TKey>
        where TKey : notnull
    {
        ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key,
            IComputation? dependant = null,
            CancellationToken cancellationToken = default);
    }

    public abstract class FunctionBase<TKey, TValue> : AsyncDisposableBase,
        IFunction<TKey, TValue>
        where TKey : notnull
    {
        protected Action<IComputation> OnInvalidateHandler { get; set; }
        protected IComputationRegistry<(IFunction, TKey)> ComputationRegistry { get; }
        protected IAsyncLockSet<(IFunction, TKey)> Locks { get; }
        protected object Lock => Locks;

        public FunctionBase(
            IComputationRegistry<(IFunction, TKey)>? computationRegistry,
            IAsyncLockSet<(IFunction, TKey)>? locks = null)
        {                                                             
            computationRegistry ??= new ComputationRegistry<(IFunction, TKey)>();
            locks ??= new AsyncLockSet<(IFunction, TKey)>(ReentryMode.CheckedFail);
            ComputationRegistry = computationRegistry; 
            Locks = locks;
            OnInvalidateHandler = c => RemoveComputation((IComputation<TKey, TValue>) c);
        }

        async ValueTask<IComputed> IFunction.InvokeAsync(object key, 
            IComputation? dependant,
            CancellationToken cancellationToken) 
            => await InvokeAsync((TKey) key, dependant, cancellationToken).ConfigureAwait(false);

        async ValueTask<IComputed> IFunction<TKey>.InvokeAsync(TKey key, 
            IComputation? dependant, 
            CancellationToken cancellationToken) 
            => await InvokeAsync(key, dependant, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key, 
            IComputation? dependant = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out var computation))
                return computation;

            using var @lock = await Locks.LockAsync((this, key), cancellationToken).ConfigureAwait(false);
            
            maybeComputation = TryGetComputation(key);
            if (maybeComputation.IsSome(out computation))
                return computation;

            computation = await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            computation.Invalidated += OnInvalidateHandler;
            if (dependant != null) {
                dependant.AddDependency(computation);
                computation.AddDependant(dependant);
            }
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

        protected abstract ValueTask<IComputation<TKey, TValue>> ComputeAsync(
            TKey key, CancellationToken cancellationToken);

        protected Option<IComputation<TKey, TValue>> TryGetComputation(TKey key) 
            => ComputationRegistry.TryGetComputation((this, key)).IsSome(out var c)
                ? Option.Some((IComputation<TKey, TValue>) c)
                : Option<IComputation<TKey, TValue>>.None;

        protected void StoreComputation(IComputation<TKey, TValue> computation) 
            => ComputationRegistry.StoreComputation((this, computation.Key), computation);

        protected void RemoveComputation(IComputation<TKey, TValue> computation) 
            => ComputationRegistry.RemoveComputation((this, computation.Key), computation);
    }
}
