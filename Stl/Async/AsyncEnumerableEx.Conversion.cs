using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    // Temporary impl. -- there is nothing similar in BCL yet,
    // but we need something.
    //
    // Thus performance is not a priority for now. 
    public static partial class AsyncEnumerableEx
    {
        public const int DefaultBufferSize = 16;

        private static IMemoryOwner<T> LeaseMemory<T>(int bufferSize) => 
            MemoryPool<T>.Shared.Rent(bufferSize);
        private static IMemoryOwner<(T Item, ExceptionDispatchInfo? Error)> LeaseChannelMemory<T>(int bufferSize) => 
            MemoryPool<(T, ExceptionDispatchInfo?)>.Shared.Rent(bufferSize);

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

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IObservable<T> source,
            int bufferSize = DefaultBufferSize,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var lease = LeaseChannelMemory<T>(bufferSize);
            var channel = new AsyncChannel<(T Item, ExceptionDispatchInfo? Error)>(lease.Memory);
            source.Subscribe(
                item => channel
                    // Ok to do that: PutAsync continuation (if any)
                    // will complete on the thread pool.
                    .PutAsync((item, null), cancellationToken)
                    .Ignore(),
                e => channel
                    // Might trigger event reordering due to race between
                    // PutAsync continuations & this method, but that's
                    // the best we can do here anyway. The best way
                    // to address that is to have a larger buffer.
                    .PutAsync((default!, ExceptionDispatchInfo.Capture(e)), CancellationToken.None)
                    .AsTask()
                    .ContinueWith(_ => channel.CompletePut(), CancellationToken.None),
                // Similarly, might trigger reordering.
                () => channel.CompletePut());

            var enumerable = channel
                .ToAsyncEnumerable(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
            await foreach(var item in enumerable)
                yield return item;
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> source,
            int bufferSize = DefaultBufferSize,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var lease = LeaseChannelMemory<T>(bufferSize);
            var channel = new AsyncChannel<(T Item, ExceptionDispatchInfo? Error)>(lease.Memory);

            Task.Run(async () => {
                try {
                    foreach (var item in source)
                        await channel
                            .PutAsync((item, null), cancellationToken)
                            .ConfigureAwait(false);
                }
                catch (Exception e) {
                    await channel
                        .PutAsync((default!, ExceptionDispatchInfo.Capture(e)), cancellationToken)
                        .ConfigureAwait(false);
                }
                finally {
                    channel.CompletePut();
                }
            }, cancellationToken).Ignore();


            var enumerable = channel
                .ToAsyncEnumerable(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
            await foreach(var item in enumerable)
                yield return item;
        }

        public static IObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
        {
            return Observable.Create<T>(async (observer, cancellationToken) => {
                try {
                    await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                        observer.OnNext(item);
                    observer.OnCompleted();
                }
                catch (Exception e) {
                    observer.OnError(e);
                }
            });
        }

        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source) => 
            source.ToObservable().ToEnumerable();
    }
}
