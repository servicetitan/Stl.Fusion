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
        Task<IComputed> InvokeAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task InvokeAndStripAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        IComputed? TryGetCached(ComputedInput input);
    }

    public interface IFunction<in TIn, TOut> : IFunction
        where TIn : ComputedInput
    {
        Task<IComputed<TOut>> InvokeAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task<TOut> InvokeAndStripAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        IComputed<TOut>? TryGetCached(TIn input);
    }

    public abstract class FunctionBase<TIn, TOut> : AsyncDisposableBase,
        IFunction<TIn, TOut>
        where TIn : ComputedInput
    {
        protected Action<IComputed?>? InvalidatedHandler { get; set; }
        protected IComputedRegistry ComputedRegistry { get; }
        protected IAsyncLockSet<ComputedInput> Locks { get; }
        protected object Lock => Locks;

        protected FunctionBase(IComputedRegistry computedRegistry)
        {
            ComputedRegistry = computedRegistry;
            Locks = computedRegistry.GetLocksFor(this);
            InvalidatedHandler = c => Unregister((IComputed<TIn, TOut>) c!);
        }

        async Task<IComputed> IFunction.InvokeAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAsync((TIn) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        public virtual async Task<IComputed<TOut>> InvokeAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetCached(input);
            if (result.TryUseCached(context, usedBy))
                return result!;

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            result = TryGetCached(input);
            if (result.TryUseCached(context, usedBy))
                return result!;

            result = await ComputeAsync(input, result, cancellationToken).ConfigureAwait(false);
            if (InvalidatedHandler != null)
                result.Invalidated += InvalidatedHandler;
            Register((IComputed<TIn, TOut>) result);
            result.UseNew(context, usedBy);
            return result;
        }

        Task IFunction.InvokeAndStripAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAndStripAsync((TIn) input, usedBy, context, cancellationToken);

        public virtual async Task<TOut> InvokeAndStripAsync(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetCached(input);
            if (result.TryUseCached(context, usedBy))
                return result.Strip();

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            result = TryGetCached(input);
            if (result.TryUseCached(context, usedBy))
                return result.Strip();

            result = await ComputeAsync(input, result, cancellationToken).ConfigureAwait(false);
            if (InvalidatedHandler != null)
                result.Invalidated += InvalidatedHandler;
            Register((IComputed<TIn, TOut>) result);
            result.UseNew(context, usedBy);
            return result.Strip();
        }

        IComputed? IFunction.TryGetCached(ComputedInput input)
            => TryGetCached((TIn) input);
        public virtual IComputed<TOut>? TryGetCached(TIn input)
            => ComputedRegistry.TryGet(input) as IComputed<TIn, TOut>;

        // Protected & private

        protected abstract ValueTask<IComputed<TOut>> ComputeAsync(
            TIn input, IComputed<TOut>? cached, CancellationToken cancellationToken);

        protected void Register(IComputed<TIn, TOut> computed)
            => ComputedRegistry.Store(computed);

        protected void Unregister(IComputed<TIn, TOut> computed)
            => ComputedRegistry.Remove(computed);
    }
}
