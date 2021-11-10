namespace Stl.Channels;

public static class EnumerableExt
{
    private static readonly UnboundedChannelOptions DefaultUnboundedChannelOptions = new();

    // Buffer

    public static IAsyncEnumerable<T> Buffer<T>(
        this IAsyncEnumerable<T> source,
        int bufferSize,
        CancellationToken cancellationToken = default)
        => source.Buffer(bufferSize, true, cancellationToken);

    public static IAsyncEnumerable<T> Buffer<T>(
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

    public static IAsyncEnumerable<T> Buffer<T>(
        this IAsyncEnumerable<T> source,
        BoundedChannelOptions options,
        CancellationToken cancellationToken = default)
    {
        var buffer = source.ToBoundedChannel(options);
        return buffer.Reader.ReadAllAsync(cancellationToken);
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
