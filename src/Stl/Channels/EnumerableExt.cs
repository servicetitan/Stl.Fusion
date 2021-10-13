using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Stl.Channels
{
    public static class EnumerableExt
    {
        // CopyTo

        public static async Task CopyTo<T>(this IEnumerable<T> source,
            ChannelWriter<T> writer,
            ChannelCompletionMode channelCompletionMode,
            CancellationToken cancellationToken = default)
        {
            try {
                foreach (var item in source)
                    await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                if ((channelCompletionMode & ChannelCompletionMode.Complete) != 0)
                    writer.TryComplete();
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                if (channelCompletionMode == ChannelCompletionMode.CompleteAndPropagateError)
                    writer.TryComplete(e);
                else
                    throw;
            }
        }

        public static async Task CopyTo<T>(this IAsyncEnumerable<T> source,
            ChannelWriter<T> writer,
            ChannelCompletionMode channelCompletionMode,
            CancellationToken cancellationToken = default)
        {
            try {
                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                    await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                if ((channelCompletionMode & ChannelCompletionMode.Complete) != 0)
                    writer.TryComplete();
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                if (channelCompletionMode == ChannelCompletionMode.CompleteAndPropagateError)
                    writer.TryComplete(e);
                else
                    throw;
            }
        }

        // ToUnboundedChannel

        public static Task<Channel<T>> ToUnboundedChannel<T>(
            this IEnumerable<T> source,
            CancellationToken cancellationToken = default)
            => source.ToUnboundedChannel(null, cancellationToken);

        public static async Task<Channel<T>> ToUnboundedChannel<T>(
            this IEnumerable<T> source,
            UnboundedChannelOptions? options,
            CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<T>(options ?? new UnboundedChannelOptions());
            await source.CopyTo(channel, ChannelCompletionMode.CompleteAndPropagateError, cancellationToken)
                .ConfigureAwait(false);
            return channel;
        }

        public static Task<Channel<T>> ToUnboundedChannel<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
            => source.ToUnboundedChannel(null, cancellationToken);

        public static async Task<Channel<T>> ToUnboundedChannel<T>(
            this IAsyncEnumerable<T> source,
            UnboundedChannelOptions? options,
            CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<T>(options ?? new UnboundedChannelOptions());
            await source.CopyTo(channel, ChannelCompletionMode.CompleteAndPropagateError, cancellationToken)
                .ConfigureAwait(false);
            return channel;
        }
    }
}
