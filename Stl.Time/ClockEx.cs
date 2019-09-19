using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time.Clocks;

namespace Stl.Time
{
    public static class ClockEx
    {
        public static Task DelayAsync(this IClock clock, Moment dueAt, CancellationToken cancellationToken = default)
            => clock.DelayAsync((dueAt - clock.Now).NonNegative(), cancellationToken);
        public static Task DelayAsync(this IClock clock, long dueInMilliseconds, CancellationToken cancellationToken = default)
        {
            if (dueInMilliseconds == Timeout.Infinite)
                return clock.DelayAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            if (dueInMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(dueInMilliseconds));
            return clock.DelayAsync(TimeSpan.FromMilliseconds(dueInMilliseconds), cancellationToken);
        }

        public static IObservable<long> Timer(this IClock clock, long delayInMilliseconds)
            => clock.Timer(TimeSpan.FromMilliseconds(delayInMilliseconds));
        public static IObservable<long> Timer(this IClock clock, TimeSpan dueIn)
        {
            if (clock is RealTimeClock)
                return Observable.Timer(dueIn); // Perf. optimization
            return Observable.Create<long>(async observer => {
                var completed = false;
                try {
                    await clock.DelayAsync(dueIn).ConfigureAwait(false);
                    observer.OnNext(0);
                    completed = true;
                    observer.OnCompleted();
                }
                catch (Exception e) {
                    if (!completed)
                        observer.OnError(e);
                }
            });
        }

        public static IObservable<long> Interval(this IClock clock, long delayInMilliseconds)
            => clock.Interval(TimeSpan.FromMilliseconds(delayInMilliseconds));
        public static IObservable<long> Interval(this IClock clock, TimeSpan dueIn)
        {
            if (clock is RealTimeClock)
                return Observable.Interval(dueIn); // Perf. optimization
            return Observable.Create<long>(async (observer, ct) => {
                var completed = false;
                try {
                    var dueAt = clock.Now + dueIn;
                    for (var index = 0L;; index++, dueAt += dueIn) {
                        await clock.DelayAsync(dueAt, ct).SuppressCancellation().ConfigureAwait(false);
                        if (ct.IsCancellationRequested)
                            break;
                        observer.OnNext(index);
                    }
                    completed = true;
                    observer.OnCompleted();
                }
                catch (Exception e) {
                    if (!completed)
                        observer.OnError(e);
                }
            });
        }
    }
}
