namespace Stl.Channels;

public static partial class ChannelExt
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this ChannelReader<T> channel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (channel.TryRead(out var item))
            yield return item;
    }

    public static async Task Copy<T>(
        this ChannelReader<T> reader,
        ChannelWriter<T> writer,
        ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
        CancellationToken cancellationToken = default)
    {
        try {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out var value))
                await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
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

    public static Task Connect<T>(
        this Channel<T> channel1,
        Channel<T> channel2,
        ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(
            Task.Run(() => channel1.Reader.Copy(
                channel2, channelCompletionMode, cancellationToken), CancellationToken.None),
            Task.Run(() => channel2.Reader.Copy(
                channel1, channelCompletionMode, cancellationToken), CancellationToken.None)
        );

    public static Task Connect<T1, T2>(
        this Channel<T1> channel1, Channel<T2> channel2,
        Func<T1, T2> adapter12, Func<T2, T1> adapter21,
        ChannelCompletionMode channelCompletionMode = ChannelCompletionMode.CompleteAndPropagateError,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(
            Task.Run(() => channel1.Reader.Transform(
                channel2, adapter12, channelCompletionMode, cancellationToken), CancellationToken.None),
            Task.Run(() => channel2.Reader.Transform(
                channel1, adapter21, channelCompletionMode, cancellationToken), CancellationToken.None)
        );

    public static async Task Consume<T>(
        this ChannelReader<T> reader,
        CancellationToken cancellationToken = default)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out _)) {
            // Do nothing
        }
    }

    public static async Task ConsumeSilent<T>(
        this ChannelReader<T> reader,
        CancellationToken cancellationToken = default)
    {
        try {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out _)) {
                // Do nothing
            }
        }
        catch {
            // Silent means silent :)
        }
    }

    public static Channel<T> WithUtf16Serializer<T>(
        this Channel<string> downstreamChannel,
        IUtf16Serializer<T> serializer,
        BoundedChannelOptions? channelOptions = null,
        CancellationToken cancellationToken = default)
    {
        channelOptions ??= new BoundedChannelOptions(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        var pair = ChannelPair.CreateTwisted(
            Channel.CreateBounded<T>(channelOptions),
            Channel.CreateBounded<T>(channelOptions));

        downstreamChannel.Connect(pair.Channel1,
            serializer.Reader.Read,
            serializer.Writer.Write,
            ChannelCompletionMode.CompleteAndPropagateError,
            cancellationToken);
        return pair.Channel2;
    }


    public static Channel<T> WithByteSerializer<T>(
        this Channel<ReadOnlyMemory<byte>> downstreamChannel,
        ByteSerializer<T> serializer,
        BoundedChannelOptions? channelOptions = null,
        CancellationToken cancellationToken = default)
    {
        channelOptions ??= new BoundedChannelOptions(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        var pair = ChannelPair.CreateTwisted(
            Channel.CreateBounded<T>(channelOptions),
            Channel.CreateBounded<T>(channelOptions));

        downstreamChannel.Connect(pair.Channel1,
            serializer.Reader.Read,
            Write,
            ChannelCompletionMode.CompleteAndPropagateError,
            cancellationToken);
        return pair.Channel2;

        ReadOnlyMemory<byte> Write(T value) {
            using var bufferWriter = serializer.Writer.Write(value);
            return bufferWriter.WrittenMemory.ToArray();
        }
    }

    public static Channel<T> WithLogger<T>(
        this Channel<T> channel,
        string channelName,
        ILogger logger, LogLevel logLevel, int? maxLength = null,
        BoundedChannelOptions? channelOptions = null,
        CancellationToken cancellationToken = default)
    {
        var mustLog = logLevel != LogLevel.None && logger.IsEnabled(logLevel);
        if (!mustLog)
            return channel;

        channelOptions ??= new BoundedChannelOptions(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        var pair = ChannelPair.CreateTwisted(
            Channel.CreateBounded<T>(channelOptions),
            Channel.CreateBounded<T>(channelOptions));

        T LogMessage(T message, bool isIncoming)
        {
            var text = message?.ToString() ?? "<null>";
            if (maxLength.HasValue && text.Length > maxLength.GetValueOrDefault())
                text = text.Substring(0, maxLength.GetValueOrDefault()) + "...";
            logger.Log(logLevel, $"{channelName} {(isIncoming ? "<-" : "->")} {text}");
            return message;
        }

        channel.Connect(pair.Channel1,
            m => LogMessage(m, true),
            m => LogMessage(m, false),
            ChannelCompletionMode.CompleteAndPropagateError,
            cancellationToken);
        return pair.Channel2;
    }

    public static CustomChannelWithId<TId, T> WithId<TId, T>(
        this Channel<T> channel, TId id)
        => new(id, channel.Reader, channel.Writer);
}
