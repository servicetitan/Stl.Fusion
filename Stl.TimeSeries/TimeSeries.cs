using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.TimeSeries
{
    public static class TimeSeries
    {
        public static IAsyncEnumerable<Point<T>> ToTimeSeries<T>(
            this IObservable<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            => source
                .ToAsyncEnumerable(cancellationToken: cancellationToken)
                .ToTimeSeries(cancellationToken: cancellationToken);

        public static IAsyncEnumerable<Point<T>> ToTimeSeries<T>(
            this IAsyncEnumerable<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            => source
                .Select(x => Point.New(Time.Now, x), cancellationToken);

        public static async IAsyncEnumerable<Point<T>> CloseGaps<T>(
            this IAsyncEnumerable<Point<T>> source, TimeSpan maxTimeGap,
            Func<Point<T>, Time, T> gapValueSelector,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var lastPoint = default(Point<T>);
            var first = true;
            await foreach (var p in source.WithCancellation(cancellationToken)) {
                if (!first) {
                    while (p.Time - lastPoint.Time > maxTimeGap) {
                        var time = p.Time + maxTimeGap;
                        lastPoint = Point.New(time, gapValueSelector(lastPoint, time));
                        yield return lastPoint;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                else {
                    first = false;
                }
                lastPoint = p;
            }
        }
    }
}
