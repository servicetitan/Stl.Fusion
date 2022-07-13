namespace Stl.Channels;

public static class EnumerableExt
{
    private static readonly UnboundedChannelOptions DefaultUnboundedChannelOptions = new();

    // WithBuffer

    public static IAsyncEnumerable<T> WithBuffer<T>(
        this IAsyncEnumerable<T> source,
        int bufferSize,
        CancellationToken cancellationToken = default)
        => source.WithBuffer(bufferSize, true, cancellationToken);

    public static IAsyncEnumerable<T> WithBuffer<T>(
        this IAsyncEnumerable<T> source,
        int bufferSize,
        bool allowSynchronousContinuations,
        CancellationToken cancellationToken = default)
    {
        var buffer = source.ToBoundedChannel(new(bufferSize) {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = allowSynchronousContinuations,
        });
        return buffer.Reader.ReadAllAsync(cancellationToken);
    }

    public static IAsyncEnumerable<T> WithBuffer<T>(
        this IAsyncEnumerable<T> source,
        BoundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var buffer = source.ToBoundedChannel(options);
        return buffer.Reader.ReadAllAsync(cancellationToken);
    }

    // WithTimeout

    public static IAsyncEnumerable<T> WithItemTimeout<T>(
        this IAsyncEnumerable<T> source,
        TimeSpan itemTimeout,
        CancellationToken cancellationToken = default)
        => source.WithItemTimeout(itemTimeout, itemTimeout, MomentClockSet.Default.CpuClock, cancellationToken);

    public static IAsyncEnumerable<T> WithItemTimeout<T>(
        this IAsyncEnumerable<T> source,
        TimeSpan firstItemTimeout,
        TimeSpan itemTimeout,
        CancellationToken cancellationToken = default)
        => source.WithItemTimeout(firstItemTimeout, itemTimeout, MomentClockSet.Default.CpuClock, cancellationToken);

    public static IAsyncEnumerable<T> WithItemTimeout<T>(
        this IAsyncEnumerable<T> source,
        TimeSpan itemTimeout,
        IMomentClock clock,
        CancellationToken cancellationToken = default)
        => source.WithItemTimeout(itemTimeout, itemTimeout, clock, cancellationToken);

    public static async IAsyncEnumerable<T> WithItemTimeout<T>(
        this IAsyncEnumerable<T> source,
        TimeSpan firstItemTimeout,
        TimeSpan itemTimeout,
        IMomentClock clock,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var e = source.GetAsyncEnumerator(cancellationToken);
        await using var _ = e.ConfigureAwait(false);

        var nextTimeout = firstItemTimeout;
        while (true) {
            var hasMoreTask = e.MoveNextAsync(cancellationToken);
            var hasMore = hasMoreTask.IsCompleted
                ? await hasMoreTask.ConfigureAwait(false)
                : await hasMoreTask.AsTask()
                    .WaitAsync(clock, nextTimeout, cancellationToken)
                    .ConfigureAwait(false);
            if (hasMore)
                yield return e.Current;
            else
                yield break;
            nextTimeout = itemTimeout;
        }
    }

    // ToResults

    public static async IAsyncEnumerable<Result<T>> ToResults<T>(
        this IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var e = source.GetAsyncEnumerator(cancellationToken);
        await using var _ = e.ConfigureAwait(false);

        Result<T> item = default;
        while (true) {
            var hasMore = false;
            try {
                hasMore = await e.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                if (hasMore)
                    item = e.Current;
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                item = new Result<T>(default!, ex);
            }

            if (item.HasError) {
                yield return item;
                yield break;
            }
            if (hasMore)
                yield return item;
            else
                yield break;
        }
    }

    // TrimOnCancellation

    public static async IAsyncEnumerable<T> TrimOnCancellation<T>(
        this IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var e = source.GetAsyncEnumerator(cancellationToken);
        await using var _ = e.ConfigureAwait(false);

        while (true) {
            bool hasMore;
            T item = default!;
            try {
                hasMore = await e.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                if (hasMore)
                    item = e.Current;
            }
            catch (OperationCanceledException) {
                yield break;
            }
            if (hasMore)
                yield return item;
            else
                yield break;
        }
    }

    // CopyTo

    public static async Task CopyTo<T>(this IEnumerable<T> source,
        ChannelWriter<T> writer,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        try {
            foreach (var item in source)
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
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

    public static async Task CopyTo<T>(this IAsyncEnumerable<T> source,
        ChannelWriter<T> writer,
        ChannelCompletionMode channelCompletionMode,
        CancellationToken cancellationToken = default)
    {
        try {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
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

    // ToUnboundedChannel

    public static Channel<T> ToUnboundedChannel<T>(
        this IEnumerable<T> source,
        CancellationToken cancellationToken = default)
        => source.ToUnboundedChannel(DefaultUnboundedChannelOptions, cancellationToken);

    public static Channel<T> ToUnboundedChannel<T>(
        this IEnumerable<T> source,
        UnboundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<T>(options);
        _ = Task.Run(() => source.CopyTo(channel, ChannelCompletionMode.Full, cancellationToken), cancellationToken);
        return channel;
    }

    public static Channel<T> ToUnboundedChannel<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
        => source.ToUnboundedChannel(DefaultUnboundedChannelOptions, cancellationToken);

    public static Channel<T> ToUnboundedChannel<T>(
        this IAsyncEnumerable<T> source,
        UnboundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<T>(options);
        _ = Task.Run(() => source.CopyTo(channel, ChannelCompletionMode.Full, cancellationToken), cancellationToken);
        return channel;
    }

    // ToBoundedChannel

    public static Channel<T> ToBoundedChannel<T>(
        this IEnumerable<T> source,
        BoundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<T>(options);
        _ = Task.Run(() => source.CopyTo(channel, ChannelCompletionMode.Full, cancellationToken), cancellationToken);
        return channel;
    }

    public static Channel<T> ToBoundedChannel<T>(
        this IAsyncEnumerable<T> source,
        BoundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<T>(options);
        _ = Task.Run(() => source.CopyTo(channel, ChannelCompletionMode.Full, cancellationToken), cancellationToken);
        return channel;
    }
}
