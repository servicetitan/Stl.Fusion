using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Channels;

namespace Stl.Async
{
    // Temporary impl. -- there is nothing similar in BCL yet,
    // but we need something.
    //
    // Thus performance is not a priority for now. 
    public static partial class AsyncEnumerableEx
    {
        public const int DefaultBufferSize = 16;

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IObservable<T> source,
            BoundedChannelOptions? options = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<(T Item, ExceptionDispatchInfo? Error)>(
                options ??= new BoundedChannelOptions(DefaultBufferSize));
            var writer = channel.Writer;
            source.Subscribe(
                item => writer
                    // Ok to do that: PutAsync continuation (if any)
                    // will complete on the thread pool.
                    .WriteAsync((item, null), cancellationToken)
                    .Ignore(),
                e => writer
                    // Might trigger event reordering due to race between
                    // PutAsync continuations & this method, but that's
                    // the best we can do here anyway. The best way
                    // to address that is to have a larger buffer.
                    .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, CancellationToken.None)
                    .AsTask()
                    .ContinueWith(_ => writer.Complete(), CancellationToken.None),
                // Similarly, might trigger reordering.
                () => writer.Complete());

            var enumerable = channel
                .ToAsyncEnumerable(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
            await foreach(var item in enumerable)
                yield return item;
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IObservable<T> source,
            UnboundedChannelOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<(T Item, ExceptionDispatchInfo? Error)>(options);
            var writer = channel.Writer;
            source.Subscribe(
                item => writer
                    // Ok to do that: PutAsync continuation (if any)
                    // will complete on the thread pool.
                    .WriteAsync((item, null), cancellationToken)
                    .Ignore(),
                e => writer
                    // Might trigger event reordering due to race between
                    // PutAsync continuations & this method, but that's
                    // the best we can do here anyway. The best way
                    // to address that is to have a larger buffer.
                    .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, CancellationToken.None)
                    .AsTask()
                    .ContinueWith(_ => writer.Complete(), CancellationToken.None),
                // Similarly, might trigger reordering.
                () => writer.Complete());

            var enumerable = channel
                .ToAsyncEnumerable(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
            await foreach(var item in enumerable)
                yield return item;
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> source,
            BoundedChannelOptions? options = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<(T Item, ExceptionDispatchInfo? Error)>(
                options ??= new BoundedChannelOptions(DefaultBufferSize));
            var writer = channel.Writer;
            Task.Run(async () => {
                try {
                    foreach (var item in source)
                        await writer
                            .WriteAsync((item, null), cancellationToken)
                            .ConfigureAwait(false);
                }
                catch (Exception e) {
                    await writer
                        .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally {
                    writer.Complete();
                }
            }, cancellationToken).Ignore();

            var enumerable = channel
                .ToAsyncEnumerable(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
            await foreach(var item in enumerable)
                yield return item;
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> source,
            UnboundedChannelOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<(T Item, ExceptionDispatchInfo? Error)>(options);
            var writer = channel.Writer;
            Task.Run(async () => {
                try {
                    foreach (var item in source)
                        await writer
                            .WriteAsync((item, null), cancellationToken)
                            .ConfigureAwait(false);
                }
                catch (Exception e) {
                    await writer
                        .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally {
                    writer.Complete();
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
