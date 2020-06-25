using System;
using System.Collections.Generic;
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
        public static async IAsyncEnumerable<(long Index, T Item)> Index<T>(
            this IAsyncEnumerable<T> source, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var index = 0L;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                yield return (index++, item);
        }

        public static async Task<long> Count<T>(
            this IAsyncEnumerable<T> source, 
            CancellationToken cancellationToken = default)
        {
            var count = 0L;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                count++;
            return count;
        }

        public static async Task<long> Count<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            var count = 0L;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (predicate.Invoke(item))
                    count++;
            return count;
        }

        public static async Task<long> Count<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var count = 0L;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (await predicate.Invoke(item))
                    count++;
            return count;
        }

        public static async IAsyncEnumerable<TNew> Select<T, TNew>(
            this IAsyncEnumerable<T> source, 
            Func<T, TNew> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                yield return selector(item);
        }

        public static async IAsyncEnumerable<TNew> Select<T, TNew>(
            this IAsyncEnumerable<T> source, 
            Func<T, ValueTask<TNew>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                yield return await selector(item);
        }

        public static async IAsyncEnumerable<T> Where<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, bool> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (predicate(item))
                    yield return item;
        }

        public static async IAsyncEnumerable<T> Where<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, ValueTask<bool>> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (await predicate(item))
                    yield return item;
        }

        public static async IAsyncEnumerable<T> TakeWhile<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, bool> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (predicate(item))
                    yield return item;
                else
                    break;
        }

        public static async IAsyncEnumerable<T> TakeWhile<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, ValueTask<bool>> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (await predicate(item).ConfigureAwait(false))
                    yield return item;
                else
                    break;
        }

        public static IAsyncEnumerable<T> Take<T>(
            this IAsyncEnumerable<T> source, 
            long count) =>
            source
                .Index()
                .TakeWhile(p => p.Index < count)
                .Select(p => p.Item);
            
        public static async IAsyncEnumerable<T> SkipWhile<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, bool> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            bool skipping = true;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (skipping) {
                    if (!predicate(item)) {
                        yield return item;
                        skipping = false;
                    }
                }
                else
                    yield return item;
        }

        public static async IAsyncEnumerable<T> SkipWhile<T>(
            this IAsyncEnumerable<T> source, 
            Func<T, ValueTask<bool>> predicate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            bool skipping = true;
            await foreach(var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                if (skipping) {
                    if (!await predicate(item).ConfigureAwait(false)) {
                        yield return item;
                        skipping = false;
                    }
                }
                else
                    yield return item;
        }

        public static IAsyncEnumerable<T> Skip<T>(
            this IAsyncEnumerable<T> source, 
            long count) =>
            source
                .Index()
                .SkipWhile(p => p.Index < count)
                .Select(p => p.Item);

        public static async IAsyncEnumerable<TNew> SelectMany<T, TNew>(
            this IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<TNew>> selector,
            BoundedChannelOptions? options = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<(TNew Item, ExceptionDispatchInfo? Error)>(
                options ??= new BoundedChannelOptions(DefaultBufferSize));
            var writer = channel.Writer;
            Task.Run(async () => {
                try {
                    await foreach(var i1 in source.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        Task.Run(async () => {
                            try {
                                var subsequence = selector(i1);
                                await foreach (var i2 in subsequence.WithCancellation(cancellationToken).ConfigureAwait(false))
                                    await writer.WriteAsync((i2, null), cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e) {
                                // Catching errors from the subsequence
                                await writer
                                    .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                                    .ConfigureAwait(false);
                                writer.Complete();
                            }
                        }, cancellationToken).Ignore();
                    }
                }
                catch (Exception e) {
                    // Catching errors from the main sequence 
                    await writer
                        .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                        .ConfigureAwait(false);
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

        public static async IAsyncEnumerable<TNew> SelectMany<T, TNew>(
            this IAsyncEnumerable<T> source, Func<T, IEnumerable<TNew>> selector,
            BoundedChannelOptions? options = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<(TNew Item, ExceptionDispatchInfo? Error)>(
                options ??= new BoundedChannelOptions(DefaultBufferSize));
            var writer = channel.Writer;
            Task.Run(async () => {
                try {
                    await foreach(var i1 in source.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        Task.Run(async () => {
                            try {
                                var subsequence = selector(i1);
                                foreach (var i2 in subsequence)
                                    await writer.WriteAsync((i2, null), cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e) {
                                // Catching errors from the subsequence
                                await writer
                                    .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                                    .ConfigureAwait(false);
                                writer.Complete();
                            }
                        }, cancellationToken).Ignore();
                    }
                }
                catch (Exception e) {
                    // Catching errors from the main sequence 
                    await writer
                        .WriteAsync((default, ExceptionDispatchInfo.Capture(e))!, cancellationToken)
                        .ConfigureAwait(false);
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

        public static async IAsyncEnumerable<(T1, T2, bool)> Stitch<T1, T2>(
            this IAsyncEnumerable<T1> source1, IAsyncEnumerable<T2> source2,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var e1 = source1.GetAsyncEnumerator(cancellationToken);
            await using var e2 = source2.GetAsyncEnumerator(cancellationToken);
            var v1 = default(T1)!;
            var v2 = default(T2)!;
            while (true) {
                // TODO: Get rid of excessive Task allocations
                var t1 = e1.MoveNextAsync().AsTask();
                var t2 = e2.MoveNextAsync().AsTask();
                var t = await Task.WhenAny(t1, t2).ConfigureAwait(false);
                if (t == t1) {
                    if (!t.Result) {
                        while (await t2.ConfigureAwait(false))
                            yield return (v1, e2.Current, true);
                        yield break;
                    }
                    v1 = e1.Current;
                    yield return (v1, v2, false); 
                } else {
                    if (!t.Result) {
                        while (await t1.ConfigureAwait(false))
                            yield return (e1.Current, v2, false);
                        yield break;
                    }
                    v2 = e2.Current;
                    yield return (v1, v2, true); 
                }
            }
        }
    }
}
