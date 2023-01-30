using Stl.OS;

namespace Stl.Channels;

public static partial class ChannelExt
{
    // Transform

    public static async Task Transform<TIn, TOut>(
        this ChannelReader<TIn> reader,
        ChannelWriter<TOut> writer,
        Func<TIn, TOut> transformer,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        try {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out var item)) {
                var newItem = transformer(item);
                await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
            }
            if ((channelCompletionMode & ChannelCompletionMode.PropagateCompletion) != 0)
                writer.TryComplete();
        }
        catch (OperationCanceledException oce) {
            if ((channelCompletionMode & ChannelCompletionMode.PropagateCancellation) != 0)
                writer.TryComplete(oce);
            throw;
        }
        catch (Exception e) {
            if ((channelCompletionMode & ChannelCompletionMode.PropagateError) != 0)
                writer.TryComplete(e);
            throw;
        }
    }

    public static async Task Transform<TIn, TOut>(
        this ChannelReader<TIn> reader,
        ChannelWriter<TOut> writer,
        Func<TIn, ValueTask<TOut>> transformer,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        try {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out var item)) {
                var newItem = await transformer(item).ConfigureAwait(false);
                await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
            }
            if ((channelCompletionMode & ChannelCompletionMode.PropagateCompletion) != 0)
                writer.TryComplete();
        }
        catch (OperationCanceledException oce) {
            if ((channelCompletionMode & ChannelCompletionMode.PropagateCancellation) != 0)
                writer.TryComplete(oce);
            throw;
        }
        catch (Exception e) {
            if ((channelCompletionMode & ChannelCompletionMode.PropagateError) != 0)
                writer.TryComplete(e);
            throw;
        }
    }

    // ConcurrentTransform

    public static async Task ConcurrentTransform<TIn, TOut>(
        this ChannelReader<TIn> reader,
        ChannelWriter<TOut> writer,
        Func<TIn, TOut> transformer,
        int concurrencyLevel,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = HardwareInfo.GetProcessorCountFactor();
        var semaphore = new SemaphoreSlim(concurrencyLevel, concurrencyLevel);
        Exception? error = null;

        async Task Worker()
        {
            try {
                while (true) {
                    await semaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try {
                        if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                            break;
                        while (reader.TryRead(out var item)) {
                            var newItem = transformer(item);
                            await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally {
                        semaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException oce) {
                if ((channelCompletionMode & ChannelCompletionMode.PropagateCancellation) != 0)
                    writer.TryComplete(oce);
                throw;
            }
            catch (Exception e) {
                if ((channelCompletionMode & ChannelCompletionMode.PropagateError) != 0)
                    writer.TryComplete(e);
                throw;
            }
        }

        var workers = new Task[concurrencyLevel];
        for (var i = 0; i < concurrencyLevel; i++)
            workers[i] = Task.Run(Worker, cancellationToken);
        await Task.WhenAll(workers).ConfigureAwait(false);
        if ((channelCompletionMode & ChannelCompletionMode.PropagateCompletion) != 0)
            writer.TryComplete(error);
    }

    public static async Task ConcurrentTransform<TIn, TOut>(
        this ChannelReader<TIn> reader,
        ChannelWriter<TOut> writer,
        Func<TIn, ValueTask<TOut>> transformer,
        int concurrencyLevel,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = HardwareInfo.GetProcessorCountFactor();
        var semaphore = new SemaphoreSlim(concurrencyLevel, concurrencyLevel);
        Exception? error = null;

        async Task Worker()
        {
            try {
                while (true) {
                    await semaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try {
                        if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                            break;
                        while (reader.TryRead(out var item)) {
                            var newItem = await transformer(item).ConfigureAwait(false);
                            await writer.WriteAsync(newItem, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally {
                        semaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException oce) {
                if ((channelCompletionMode & ChannelCompletionMode.PropagateCancellation) != 0)
                    writer.TryComplete(oce);
                throw;
            }
            catch (Exception e) {
                if ((channelCompletionMode & ChannelCompletionMode.PropagateError) != 0)
                    writer.TryComplete(e);
                throw;
            }
        }

        var workers = new Task[concurrencyLevel];
        for (var i = 0; i < concurrencyLevel; i++)
            workers[i] = Task.Run(Worker, cancellationToken);
        await Task.WhenAll(workers).ConfigureAwait(false);
        if ((channelCompletionMode & ChannelCompletionMode.PropagateCompletion) != 0)
            writer.TryComplete(error);
    }
}
