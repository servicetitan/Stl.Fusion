using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Caching;

namespace Stl.Fusion.Internal
{
    public static class ComputedEx
    {
        internal static bool TryUseExisting<T>(this IComputed<T>? existing, ComputeContext context, IComputed? usedBy)
        {
            var callOptions = context.CallOptions;
            var useExisting = (callOptions & CallOptions.TryGetExisting) != 0;

            if (existing == null)
                return useExisting;
            useExisting |= existing.IsConsistent;
            if (!useExisting)
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

        internal static async ValueTask<ResultBox<T>?> TryUseExistingAsync<T>(
            this ICachingComputed<T>? existing, ComputeContext context, IComputed? usedBy,
            CancellationToken cancellationToken)
        {
            var callOptions = context.CallOptions;
            var useExisting = (callOptions & CallOptions.TryGetExisting) != 0;

            if (existing == null)
                return useExisting ? ResultBox<T>.Default : null;

            useExisting |= existing.IsConsistent;
            if (!useExisting)
                return null;

            if ((callOptions & CallOptions.Invalidate) == CallOptions.Invalidate) {
                existing.Invalidate();
                if ((callOptions & CallOptions.Capture) != 0)
                    Interlocked.Exchange(ref context.CapturedComputed, existing);
                return existing.MaybeOutput ?? ResultBox<T>.Default;
            }

            var result = existing.MaybeOutput;
            if (result == null) {
                result = await existing.GetOutputAsync(cancellationToken).ConfigureAwait(false);
                if (result == null)
                    return null;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed)
            => computed != null ? computed.Value : default!;    }
}
