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
            TIn input, Option<Result<object>> output,
            CancellationToken cancellationToken = default);
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

        public abstract ValueTask<Option<Result<T>>> GetCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default);

        public abstract ValueTask SetCachedOutputAsync(
            InterceptedInput input, Option<Result<object>> output,
            CancellationToken cancellationToken = default);

        public override async Task<T> InvokeAndStripAsync(
            InterceptedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;
            Result<T> output;

            // Read-Lock-RetryRead-Compute-Store pattern

            var result = TryGetExisting(input);
            if (result != null && IsCachingEnabled) {
                var maybeOutput = await result.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (maybeOutput.IsSome(out output))
                    return output.Value;
            }
            else if (result.TryUseExisting(context, usedBy))
                return result!.Value;

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            result = TryGetExisting(input);
            if (result != null && IsCachingEnabled) {
                var maybeOutput = await result.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (maybeOutput.IsSome(out output))
                    return output.Value;
            }
            else if (result.TryUseExisting(context, usedBy))
                return result!.Value;

            result = await ComputeAsync(input, result, cancellationToken).ConfigureAwait(false);
            output = result.Output; // It can't be gone here b/c KeepAlive isn't called yet
            result.UseNew(context, usedBy);
            return output.Value;
        }
    }
}
