using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion
{
    public interface IFunction : IAsyncDisposable
    {
        Task<IComputed?> InvokeAsync(ComputedInput input, 
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task InvokeAndStripAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        IComputed? TryGetCached(ComputedInput input, IComputed? usedBy);
    }

    public interface IFunction<in TIn, TOut> : IFunction
        where TIn : ComputedInput
    {
        Task<IComputed<TOut>?> InvokeAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task<TOut> InvokeAndStripAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        IComputed<TOut>? TryGetCached(TIn input, IComputed? usedBy);
    }

    public abstract class FunctionBase<TIn, TOut> : AsyncDisposableBase,
        IFunction<TIn, TOut>
        where TIn : ComputedInput
    {
        protected Action<IComputed, object?> OnInvalidateHandler { get; set; }
        protected IComputedRegistry<(IFunction, TIn)> ComputedRegistry { get; }
        protected IComputeRetryPolicy ComputeRetryPolicy { get; }
        protected IAsyncLockSet<(IFunction, TIn)> Locks { get; }
        protected object Lock => Locks;

        public FunctionBase(
            IComputedRegistry<(IFunction, TIn)> computedRegistry,
            IComputeRetryPolicy? computeRetryPolicy = null,
            IAsyncLockSet<(IFunction, TIn)>? locks = null)
        {
            computeRetryPolicy ??= Fusion.ComputeRetryPolicy.Default;
            locks ??= new AsyncLockSet<(IFunction, TIn)>(ReentryMode.CheckedFail);
            ComputedRegistry = computedRegistry;
            ComputeRetryPolicy = computeRetryPolicy; 
            Locks = locks;
            OnInvalidateHandler = (c, _) => Unregister((IComputed<TIn, TOut>) c);
        }

        async Task<IComputed?> IFunction.InvokeAsync(ComputedInput input, 
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAsync((TIn) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        public async Task<IComputed<TOut>?> InvokeAsync(TIn input, 
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result!;
            }

            using var @lock = await Locks.LockAsync((this, input), cancellationToken).ConfigureAwait(false);
            
            result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result!;
            }

            for (var tryIndex = 0;; tryIndex++) {
                result = await ComputeAsync(input, cancellationToken).ConfigureAwait(false);
                if (result.IsValid)
                    break;
                if (!ComputeRetryPolicy.MustRetry(result, tryIndex))
                    break;
            }
            context.TryCaptureValue(result);
            result.Invalidated += OnInvalidateHandler;
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            Register((IComputed<TIn, TOut>) result);
            return result;
        }

        Task IFunction.InvokeAndStripAsync(ComputedInput input,  
            IComputed? usedBy, 
            ComputeContext? context, 
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync((TIn) input, usedBy, context, cancellationToken);

        public async Task<TOut> InvokeAndStripAsync(TIn input, 
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result.Strip();
            }

            using var @lock = await Locks.LockAsync((this, input), cancellationToken).ConfigureAwait(false);
            
            result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result.Strip();
            }

            for (var tryIndex = 0;; tryIndex++) {
                result = await ComputeAsync(input, cancellationToken).ConfigureAwait(false);
                if (result.IsValid)
                    break;
                if (!ComputeRetryPolicy.MustRetry(result, tryIndex))
                    break;
            }
            context.TryCaptureValue(result);
            result.Invalidated += OnInvalidateHandler;
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            Register((IComputed<TIn, TOut>) result);
            return result.Strip();
        }

        IComputed? IFunction.TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached((TIn) input, null);
        public virtual IComputed<TOut>? TryGetCached(TIn input, IComputed? usedBy)
        {
            var value = ComputedRegistry.TryGet((this, input)) as IComputed<TIn, TOut>;
            if (value != null)
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) value);
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
