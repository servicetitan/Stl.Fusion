using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Locking;

namespace Stl.Purifier
{
    public interface IFunction : IAsyncDisposable
    {
        ValueTask<IComputed> InvokeAsync(object input, 
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        IComputed? TryGetCached(object input, 
            IComputed? usedBy = null);
    }

    public interface IFunction<in TIn> : IFunction
        where TIn : notnull
    {
        ValueTask<IComputed> InvokeAsync(TIn input,
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        IComputed? TryGetCached(TIn input, 
            IComputed? usedBy = null);
    }
    
    public interface IFunction<in TIn, TOut> : IFunction<TIn>
        where TIn : notnull
    {
        new ValueTask<IComputed<TOut>> InvokeAsync(TIn input,
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default);
        new IComputed<TOut>? TryGetCached(TIn input, 
            IComputed? usedBy = null);
    }

    public abstract class FunctionBase<TIn, TOut> : AsyncDisposableBase,
        IFunction<TIn, TOut>
        where TIn : notnull
    {
        protected Action<IComputed> OnInvalidateHandler { get; set; }
        protected IComputedRegistry<(IFunction, TIn)> ComputedRegistry { get; }
        protected IAsyncLockSet<(IFunction, TIn)> Locks { get; }
        protected object Lock => Locks;

        public FunctionBase(
            IComputedRegistry<(IFunction, TIn)> computedRegistry,
            IAsyncLockSet<(IFunction, TIn)>? locks = null)
        {                                                             
            locks ??= new AsyncLockSet<(IFunction, TIn)>(ReentryMode.CheckedFail);
            ComputedRegistry = computedRegistry; 
            Locks = locks;
            OnInvalidateHandler = c => Unregister((IComputed<TIn, TOut>) c);
        }

        async ValueTask<IComputed> IFunction.InvokeAsync(object input, 
            IComputed? usedBy,
            CancellationToken cancellationToken) 
            => await InvokeAsync((TIn) input, usedBy, cancellationToken).ConfigureAwait(false);

        async ValueTask<IComputed> IFunction<TIn>.InvokeAsync(TIn input, 
            IComputed? usedBy, 
            CancellationToken cancellationToken) 
            => await InvokeAsync(input, usedBy, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IComputed<TOut>> InvokeAsync(TIn input, 
            IComputed? usedBy = null,
            CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetCached(input, usedBy);
            if (result != null)
                return result;

            using var @lock = await Locks.LockAsync((this, input), cancellationToken).ConfigureAwait(false);
            
            result = TryGetCached(input, usedBy);
            if (result != null)
                return result;

            result = await ComputeAsync(input, cancellationToken).ConfigureAwait(false);
            result.Invalidated += OnInvalidateHandler;
            usedBy?.AddUsed(result);
            Register((IComputed<TIn, TOut>) result);
            return result;
        }

        IComputed? IFunction.TryGetCached(object input, IComputed? usedBy) 
            => TryGetCached((TIn) input);
        IComputed? IFunction<TIn>.TryGetCached(TIn input, IComputed? usedBy) 
            => TryGetCached(input);
        public IComputed<TOut>? TryGetCached(TIn input, IComputed? usedBy = null)
        {
            var value = ComputedRegistry.TryGet((this, input)) as IComputed<TIn, TOut>;
            if (value != null)
                usedBy?.AddUsed(value);
            return value;
        }

        // Protected & private

        protected abstract ValueTask<IComputed<TOut>> ComputeAsync(
            TIn input, CancellationToken cancellationToken);

        protected void Register(IComputed<TIn, TOut> computed) 
            => ComputedRegistry.Store((this, computed.Input), computed);

        protected void Unregister(IComputed<TIn, TOut> computed) 
            => ComputedRegistry.Remove((this, computed.Input), computed);
    }
}
