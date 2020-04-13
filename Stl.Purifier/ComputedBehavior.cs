using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time;

namespace Stl.Purifier
{
    public static class ComputedBehavior
    {
        public static IKeyedComputed<TKey> AutoRecompute<TKey>(this IKeyedComputed<TKey> computed, 
            TimeSpan delay = default, 
            IClock? clock = null,
            CancellationToken cancellationToken = default)
            where TKey : notnull
        {
            clock ??= RealTimeClock.Instance;
            computed.Invalidated += async c => {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var kc = (IKeyedComputed<TKey>) c;
                var (function, key) = (kc.Function, kc.Key);
                if (delay > TimeSpan.Zero)
                    await clock.DelayAsync(delay, cancellationToken)
                        .SuppressCancellation()
                        .ConfigureAwait(false);
                else
                    await Task.Yield();
                if (cancellationToken.IsCancellationRequested)
                    return;
                kc = await function.InvokeAsync(key, null, cancellationToken).AsTask()
                    .SuppressCancellation()
                    .ConfigureAwait(false);
                kc?.AutoRecompute(delay, clock, cancellationToken);
            };
            return computed; 
        }
    }
}
