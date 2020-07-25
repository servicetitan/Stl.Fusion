using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Stl.Async
{
    public static class AsyncChannelEx
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this AsyncChannel<(T Item, ExceptionDispatchInfo? Error)> channel,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true) {
                var (isPulled, (item, error)) =
                    await channel.PullAsync(cancellationToken).ConfigureAwait(false);
                if (!isPulled)
                    break;
                if (error == null)
                    yield return item;
                else
                    error.Throw();
            }
        }
    }
}
