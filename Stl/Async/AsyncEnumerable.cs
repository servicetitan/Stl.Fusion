using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class AsyncEnumerable
    {
        public static async IAsyncEnumerable<long> Interval(
            TimeSpan period,
            bool skipZero = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sequence = Interval(_ => period, cancellationToken);
            if (skipZero)
                sequence = sequence.Where(i => i > 0, cancellationToken);
            await foreach (var item in sequence.ConfigureAwait(false))
                yield return item;
        }

        public static async IAsyncEnumerable<long> Interval(
            Func<long, TimeSpan> periodProvider, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var value = 0L;
            while (true) {
                var period = periodProvider(value);
                if (period < TimeSpan.Zero)
                    break;
                yield return value++;
                await Task.Delay(period, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
