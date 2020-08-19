using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Internal
{
    public static class ComputedEx
    {
        internal static bool TryUseExisting<T>(this IComputed<T>? existing, ComputeContext context, IComputed? usedBy)
        {
            var callOptions = context.CallOptions;
            var useCached = (callOptions & CallOptions.TryGetExisting) != 0;

            if (existing == null)
                return useCached;
            useCached |= existing.IsConsistent;
            if (!useCached)
                return false;

            if ((callOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, existing);
            if ((callOptions & CallOptions.Invalidate) == CallOptions.Invalidate) {
                existing.Invalidate();
                return true;
            }
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
            existing.KeepAlive();
            return true;
        }

        internal static async ValueTask<Option<Result<T>>> TryUseExistingAsync<T>(
            this IComputed<T>? existing, ComputeContext context, IComputed? usedBy,
            CancellationToken cancellationToken)
        {
            var callOptions = context.CallOptions;
            var useCached = (callOptions & CallOptions.TryGetExisting) != 0;

            if (existing == null)
                return useCached
                    ? Option<Result<T>>.Some(default)
                    : Option<Result<T>>.None;

            useCached |= existing.IsConsistent;
            if (!useCached)
                return Option<Result<T>>.None;

            if ((callOptions & CallOptions.Invalidate) == CallOptions.Invalidate) {
                existing.Invalidate();
                if ((callOptions & CallOptions.Capture) != 0)
                    Interlocked.Exchange(ref context.CapturedComputed, existing);
                return Option<Result<T>>.Some(default);
            }

            var result = existing.MaybeOutput;
            if (!result.HasValue) {
                result = await existing.GetOutputAsync(cancellationToken).ConfigureAwait(false);
                if (!result.HasValue)
                    return Option<Result<T>>.None;
            }

            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
            if ((callOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, existing);
            existing.KeepAlive();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UseNew<T>(this IComputed<T> computed, ComputeContext context, IComputed? usedBy)
        {
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
            if ((context.CallOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, computed);
            computed.KeepAlive();
        }
    }
}
