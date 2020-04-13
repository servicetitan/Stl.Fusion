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
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        IComputed? TryGetCached(object key, 
            IComputed? usedBy = null);
    }

    public interface IFunction<TKey> : IFunction
        where TKey : notnull
    {
        ValueTask<IKeyedComputed<TKey>> InvokeAsync(TKey key,
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        IKeyedComputed<TKey>? TryGetCached(TKey key, 
            IComputed? usedBy = null);
    }
    
    public interface IFunction<TKey, TValue> : IFunction<TKey>
        where TKey : notnull
    {
        new ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key,
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        new IComputed<TKey, TValue>? TryGetCached(TKey key, 
            IComputed? usedBy = null);
    }

    public abstract class FunctionBase<TKey, TValue> : AsyncDisposableBase,
        IFunction<TKey, TValue>
        where TKey : notnull
    {
        protected Action<IComputed> OnInvalidateHandler { get; set; }
        protected IComputedRegistry<(IFunction, TKey)> ComputedRegistry { get; }
        protected IAsyncLockSet<(IFunction, TKey)> Locks { get; }
        protected object Lock => Locks;

        public FunctionBase(
            IComputedRegistry<(IFunction, TKey)>? computedRegistry,
            IAsyncLockSet<(IFunction, TKey)>? locks = null)
        {                                                             
            computedRegistry ??= new ComputedRegistry<(IFunction, TKey)>();
            locks ??= new AsyncLockSet<(IFunction, TKey)>(ReentryMode.CheckedFail);
            ComputedRegistry = computedRegistry; 
            Locks = locks;
            OnInvalidateHandler = c => Unregister((IComputed<TKey, TValue>) c);
        }

        async ValueTask<IComputed> IFunction.InvokeAsync(object key, 
            IComputed? usedBy,
            CancellationToken cancellationToken) 
            => await InvokeAsync((TKey) key, usedBy, cancellationToken).ConfigureAwait(false);

        async ValueTask<IKeyedComputed<TKey>> IFunction<TKey>.InvokeAsync(TKey key, 
            IComputed? usedBy, 
            CancellationToken cancellationToken) 
            => await InvokeAsync(key, usedBy, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IComputed<TKey, TValue>> InvokeAsync(TKey key, 
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var value = TryGetCached(key, usedBy);
            if (!value.IsNull())
                return value;

            using var @lock = await Locks.LockAsync((this, key), cancellationToken).ConfigureAwait(false);
            
            value = TryGetCached(key, usedBy);
            if (!value.IsNull())
                return value;

            value = await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            value.Invalidated += OnInvalidateHandler;
            usedBy?.AddUsedValue(value);
            Register(value);
            return value;
        }

        IComputed? IFunction.TryGetCached(object key, IComputed? usedBy) 
            => TryGetCached((TKey) key);
        IKeyedComputed<TKey>? IFunction<TKey>.TryGetCached(TKey key, IComputed? usedBy) 
            => TryGetCached(key);
        public IComputed<TKey, TValue>? TryGetCached(TKey key, IComputed? usedBy = null)
        {
            var value = ComputedRegistry.TryGet((this, key)) as IComputed<TKey, TValue>;
            if (!value.IsNull())
                usedBy?.AddUsedValue(value);
            return value;
        }

        // Protected & private

        protected abstract ValueTask<IComputed<TKey, TValue>> ComputeAsync(
            TKey key, CancellationToken cancellationToken);

        protected void Register(IComputed<TKey, TValue> computed) 
            => ComputedRegistry.Store((this, computed.Key), computed);

        protected void Unregister(IComputed<TKey, TValue> computed) 
            => ComputedRegistry.Remove((this, computed.Key), computed);
    }
}
