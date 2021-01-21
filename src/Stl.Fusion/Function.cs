using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion
{
    public interface IFunction : IHasServices, IAsyncDisposable
    {
        Task<IComputed> InvokeAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task InvokeAndStripAsync(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
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
    }

    public abstract class FunctionBase<TIn, TOut> : AsyncDisposableBase, IFunction<TIn, TOut>
        where TIn : ComputedInput
    {
        protected IAsyncLockSet<ComputedInput> Locks { get; }
        protected object Lock => Locks;
        public IServiceProvider Services { get; }

        protected FunctionBase(IServiceProvider services)
        {
            Services = services;
            Locks = ComputedRegistry.Instance.GetLocksFor(this);
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

            var result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result!;

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result!;

            result = await ComputeAsync(input, result, cancellationToken).ConfigureAwait(false);
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
            Result<TOut> output;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            result = await ComputeAsync(input, result, cancellationToken).ConfigureAwait(false);
            output = result.Output; // It can't be gone here b/c KeepAlive isn't called yet
            result.UseNew(context, usedBy);
            return output.Value;
        }

        protected IComputed<TOut>? TryGetExisting(TIn input)
        {
            var computed = ComputedRegistry.Instance.TryGet(input);
            return computed as IComputed<TIn, TOut>;
        }

        // Protected & private

        protected abstract ValueTask<IComputed<TOut>> ComputeAsync(
            TIn input, IComputed<TOut>? existing, CancellationToken cancellationToken);
    }
}
