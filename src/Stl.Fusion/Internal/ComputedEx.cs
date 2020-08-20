using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Caching;

namespace Stl.Fusion.Internal
{
    public static class ComputedEx
    {
        public static void RenewTimeouts(this IComputed computed)
        {
            var options = computed.Options;
            switch (options.IsCachingEnabled) {
            case true:
                var cachingOptions = options.CachingOptions;
                var outputReleaseTime = cachingOptions.OutputReleaseTime;
                if (outputReleaseTime == TimeSpan.MaxValue)
                    goto default;
                if (computed.State != ComputedState.Invalidated)
                    Timeouts.ReleaseOutput.AddOrUpdateToLater(
                        (ICachingComputed) computed, Timeouts.Clock.Now + outputReleaseTime);
                break;
            default:
                var keepAliveTime = options.KeepAliveTime;
                if (keepAliveTime != TimeSpan.Zero && computed.State != ComputedState.Invalidated)
                    Timeouts.KeepAlive.AddOrUpdateToLater(
                        computed, Timeouts.Clock.Now + keepAliveTime);
                break;
            }
        }

        public static void CancelTimeouts(this IComputed computed)
        {
            var options = computed.Options;
            switch (options.IsCachingEnabled) {
            case true:
                var cachingOptions = options.CachingOptions;
                var outputReleaseTime = cachingOptions.OutputReleaseTime;
                if (outputReleaseTime == TimeSpan.MaxValue)
                    goto default;
                Timeouts.ReleaseOutput.Remove((ICachingComputed) computed);
                break;
            default:
                var keepAliveTime = options.KeepAliveTime;
                if (keepAliveTime != TimeSpan.Zero)
                    Timeouts.KeepAlive.Remove(computed);
                break;
            }
        }

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
            existing.RenewTimeouts();
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
            existing.RenewTimeouts();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UseNew<T>(this IComputed<T> computed, ComputeContext context, IComputed? usedBy)
        {
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
            if ((context.CallOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, computed);
            computed.RenewTimeouts();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed)
            => computed != null ? computed.Value : default!;    }
}
