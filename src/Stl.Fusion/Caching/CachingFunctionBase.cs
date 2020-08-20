using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Caching
{
    public interface ICachingFunction<in TIn, TOut> : IFunction<TIn, TOut>
        where TIn : ComputedInput
    {
        CachingOptions CachingOptions { get; }

        ValueTask<Option<Result<TOut>>> GetCachedOutputAsync(
            TIn input, CancellationToken cancellationToken = default);
        ValueTask SetCachedOutputAsync(
            TIn input, Result<object> output,
            CancellationToken cancellationToken = default);
        ValueTask RemoveCachedOutputAsync(
            TIn input, CancellationToken cancellationToken = default);
    }

    public abstract class CachingFunctionBase<T> : InterceptedFunctionBase<T>, ICachingFunction<InterceptedInput, T>
    {
        public CachingOptions CachingOptions { get; }
        protected readonly bool IsCachingEnabled;

        protected CachingFunctionBase(InterceptedMethod method) : base(method)
        {
            CachingOptions = method.Options.CachingOptions;
            IsCachingEnabled = CachingOptions.IsCachingEnabled;
        }

        // Get-Set-RemoveCachedOutputAsync
        public abstract ValueTask<Option<Result<T>>> GetCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default);
        public abstract ValueTask SetCachedOutputAsync(
            InterceptedInput input, Result<object> output, CancellationToken cancellationToken = default);
        public abstract ValueTask RemoveCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default);

        public override Task<T> InvokeAndStripAsync(
            InterceptedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken = default)
            => IsCachingEnabled
                ? InvokeAndStripCachingAsync(input, usedBy, context, cancellationToken)
                : base.InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        protected async Task<T> InvokeAndStripCachingAsync(
            InterceptedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;
            ResultBox<T>? output;

            // Read-Lock-RetryRead-Compute-Store pattern

            var computed = TryGetExisting(input);
            if (computed != null) {
                output = await computed.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (output != null)
                    return output.Value;
            }

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            computed = TryGetExisting(input);
            if (computed != null) {
                output = await computed.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (output != null)
                    return output.Value;
            }

            computed = (ICachingComputed<T>) await ComputeAsync(input, computed, cancellationToken)
                .ConfigureAwait(false);
            output = computed.MaybeOutput; // It can't be gone here b/c KeepAlive isn't called yet
            computed.UseNew(context, usedBy);
            return output!.Value;
        }

        new protected ICachingComputed<T>? TryGetExisting(InterceptedInput input)
        {
            var computed = ComputedRegistry.Instance.TryGet(input);
            return computed as ICachingComputed<T>;
        }
    }
}
