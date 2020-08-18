using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stl.Fusion.Internal
{
    public static class ComputedEx
    {
        internal static bool TryUseCached(this IComputed? cached, ComputeContext context, IComputed? usedBy)
        {
            var callOptions = context.CallOptions;
            var useCached = (callOptions & CallOptions.TryGetCached) != 0;

            if (cached == null) {
                // AY: Below code is commented out since context.CapturedComputed
                //     should be null anyway in this case.
                // if (useCached && (callOptions & CallOptions.Capture) != 0)
                //     Interlocked.Exchange(ref context.CapturedComputed, null);
                return useCached;
            }

            useCached |= cached.IsConsistent;
            if (!useCached)
                return false;
            if ((callOptions & CallOptions.Invalidate) == CallOptions.Invalidate)
                cached.Invalidate();
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) cached!);
            if ((callOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, cached);
            cached.KeepAlive();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UseNew(this IComputed computed, ComputeContext context, IComputed? usedBy)
        {
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
            if ((context.CallOptions & CallOptions.Capture) != 0)
                Interlocked.Exchange(ref context.CapturedComputed, computed);
            computed.KeepAlive();
        }
    }
}
