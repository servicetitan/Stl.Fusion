using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.OS;

namespace Stl.Channels
{
    public static partial class ChannelEx
    {
        // Transform

        public static async Task Transform<TIn, TOut>(
            this ChannelReader<TIn> reader,
            ChannelWriter<TOut> writer,
            Func<TIn, TOut> transformer,
            ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
            CancellationToken cancellationToken = default)
        {
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (reader.TryRead(out var item)) {
                        var newItem = transformer.Invoke(item);
                        await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                    }
                }
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

        public static async Task Transform<TIn, TOut>(
            this ChannelReader<TIn> reader,
            ChannelWriter<TOut> writer,
            Func<TIn, ValueTask<TOut>> transformer,
            ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
            CancellationToken cancellationToken = default)
        {
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (reader.TryRead(out var item)) {
                        var newItem = await transformer.Invoke(item).ConfigureAwait(false);
                        await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                    }
                }
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

        // ConcurrentTransform

        public static async Task ConcurrentTransform<TIn, TOut>(
            this ChannelReader<TIn> reader,
            ChannelWriter<TOut> writer,
            Func<TIn, TOut> transformer,
            int concurrencyLevel = -1,
            ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
            CancellationToken cancellationToken = default)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = HardwareInfo.GetProcessorCountFactor();
            var semaphore = new SemaphoreSlim(concurrencyLevel, concurrencyLevel);
            Exception? error = null;

            async Task Worker()
            {
                try {
                    for (;;) {
                        await semaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try {
                            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                                break;
                            if (!reader.TryRead(out var item))
                                continue;
                            var newItem = transformer.Invoke(item);
                            await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                        }
                        finally {
                            semaphore.Release();
                        }
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    if (channelCompletionMode == ChannelCompletionMode.CompleteAndPropagateError)
                        error = e;
                    else
                        throw;
                }
            }

            var workers = new Task[concurrencyLevel];
            for (var i = 0; i < concurrencyLevel; i++)
                workers[i] = Task.Run(Worker, cancellationToken);
            await Task.WhenAll(workers).ConfigureAwait(false);
            if ((channelCompletionMode & ChannelCompletionMode.Complete) != 0)
                writer.TryComplete(error);
        }

        public static async Task ConcurrentTransform<TIn, TOut>(
            this ChannelReader<TIn> reader,
            ChannelWriter<TOut> writer,
            Func<TIn, ValueTask<TOut>> transformer,
            int concurrencyLevel = -1,
            ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
            CancellationToken cancellationToken = default)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = HardwareInfo.GetProcessorCountFactor();
            var semaphore = new SemaphoreSlim(concurrencyLevel, concurrencyLevel);
            Exception? error = null;

            async Task Worker()
            {
                try {
                    for (;;) {
                        await semaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try {
                            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                                break;
                            if (!reader.TryRead(out var item))
                                continue;
                            var newItem = await transformer.Invoke(item).ConfigureAwait(false);
                            await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                        }
                        finally {
                            semaphore.Release();
                        }
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    if (channelCompletionMode == ChannelCompletionMode.CompleteAndPropagateError)
                        error = e;
                    else
                        throw;
                }
            }

            var workers = new Task[concurrencyLevel];
            for (var i = 0; i < concurrencyLevel; i++)
                workers[i] = Task.Run(Worker, cancellationToken);
            await Task.WhenAll(workers).ConfigureAwait(false);
            if ((channelCompletionMode & ChannelCompletionMode.Complete) != 0)
                writer.TryComplete(error);
        }
    }
}
