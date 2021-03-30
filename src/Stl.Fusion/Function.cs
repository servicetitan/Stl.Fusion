using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion
{
    public interface IFunction : IHasServices
    {
        Task<IComputed> Invoke(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task InvokeAndStrip(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
    }

    public interface IFunction<in TIn, TOut> : IFunction
        where TIn : ComputedInput
    {
        Task<IComputed<TOut>> Invoke(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
        Task<TOut> InvokeAndStrip(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default);
    }

    public abstract class FunctionBase<TIn, TOut> : IFunction<TIn, TOut>
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

        async Task<IComputed> IFunction.Invoke(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken)
            => await Invoke((TIn) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        public virtual async Task<IComputed<TOut>> Invoke(TIn input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result!;

            using var @lock = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

            result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result!;

            result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result;
        }

        Task IFunction.InvokeAndStrip(ComputedInput input,
            IComputed? usedBy,
            ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAndStrip((TIn) input, usedBy, context, cancellationToken);

        public virtual async Task<TOut> InvokeAndStrip(TIn input,
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

            using var @lock = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

            result = TryGetExisting(input);
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
            output = result.Output; // It can't be gone here b/c KeepAlive isn't called yet
            result.UseNew(context, usedBy);
            return output.Value;
        }

        protected IComputed<TOut>? TryGetExisting(TIn input)
        {
            var computed = ComputedRegistry.Instance.TryGet(input);
            ComputedLog.Log($"TryGetExisting: input={input}, computed={computed}, cast={(computed as IComputed<TIn, TOut>)!=null}");
            return computed as IComputed<TIn, TOut>;
        }

        // Protected & private

        protected abstract ValueTask<IComputed<TOut>> Compute(
            TIn input, IComputed<TOut>? existing, CancellationToken cancellationToken);
    }
}
