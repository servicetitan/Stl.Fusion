using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Stl.Pooling;

namespace Stl.Net;

public class WebSocketChannel : Channel<string>, IAsyncDisposable
{
    public const int DefaultReadBufferSize = 16_384;
    public const int DefaultWriteBufferSize = 16_384;

    protected int ReadBufferSize { get; }
    protected int WriteBufferSize { get; }
    protected Channel<string> ReadChannel { get; }
    protected Channel<string> WriteChannel { get; }
    protected volatile CancellationTokenSource? StopCts;
    protected readonly CancellationToken StopToken;

    public WebSocket WebSocket { get; }
    public bool OwnsWebSocket { get; }
    public Task ReaderTask { get; }
    public Task WriterTask { get; }
    public Exception? ReaderError { get; protected set; }
    public Exception? WriterError { get; protected set; }

    public WebSocketChannel(WebSocket webSocket,
        int readBufferSize = DefaultReadBufferSize,
        int writeBufferSize = DefaultWriteBufferSize,
        Channel<string>? readChannel = null,
        Channel<string>? writeChannel = null,
        bool ownsWebSocket = true,
        BoundedChannelOptions? channelOptions = null
    )
    {
        ReadBufferSize = readBufferSize;
        WriteBufferSize = writeBufferSize;
        channelOptions ??= new BoundedChannelOptions(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        readChannel ??= Channel.CreateBounded<string>(channelOptions);
        writeChannel ??= Channel.CreateBounded<string>(channelOptions);
        WebSocket = webSocket;
        ReadChannel = readChannel;
        WriteChannel = writeChannel;
        Reader = readChannel.Reader;
        Writer = writeChannel.Writer;
        OwnsWebSocket = ownsWebSocket;

        StopCts = new CancellationTokenSource();
        var cancellationToken = StopToken = StopCts.Token;
        ReaderTask = Task.Run(() => RunReader(cancellationToken));
        WriterTask = Task.Run(() => RunWriter(cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        var stopCts = Interlocked.Exchange(ref StopCts, null!);
        if (stopCts == null)
            return;

        try {
            stopCts.Cancel();
        }
        catch {
            // Dispose shouldn't throw exceptions
        }
        try {
            await WhenCompleted(default).ConfigureAwait(false);
        }
        catch {
            // Dispose shouldn't throw exceptions
        }
        if (OwnsWebSocket)
            WebSocket.Dispose();
    }

    public Task WhenCompleted(CancellationToken cancellationToken = default)
        => Task.WhenAll(ReaderTask, WriterTask).WithFakeCancellation(cancellationToken);

    protected virtual async Task TryCloseWebSocket(CancellationToken cancellationToken)
    {
        var status = WebSocketCloseStatus.NormalClosure;
        var message = "Ok.";

        var error = ReaderError ?? WriterError;
        if (error != null) {
            status = WebSocketCloseStatus.InternalServerError;
            message = "Internal Server Error.";
        }

        await WebSocket.CloseAsync(status, message, cancellationToken).ConfigureAwait(false);
    }

    protected async Task RunReader(CancellationToken cancellationToken)
    {
        var error = (Exception?) null;
        try {
            await RunReaderUnsafe(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            error = e;
            if (!(e is OperationCanceledException))
                ReaderError = e;
            throw;
        }
        finally {
            ReadChannel.Writer.TryComplete(error);
        }
    }

    protected async Task RunWriter(CancellationToken cancellationToken)
    {
        try {
            await RunWriterUnsafe(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            if (!(e is OperationCanceledException))
                WriterError = e;
            throw;
        }
        finally {
            if (OwnsWebSocket)
                await TryCloseWebSocket(cancellationToken).ConfigureAwait(false);
        }
    }

#if !NETSTANDARD2_0

    protected virtual async Task RunReaderUnsafe(CancellationToken cancellationToken)
    {
        using var bytesOwner = MemoryPool<byte>.Shared.Rent(ReadBufferSize);
        using var charsOwner = MemoryPool<char>.Shared.Rent(ReadBufferSize);

        var decoder = Encoding.UTF8.GetDecoder();
        var writer = ReadChannel.Writer;
        var decodedPart = (StringBuilder?) null;
        var mBytes = bytesOwner.Memory;
        var mChars = charsOwner.Memory;
        var mFreeBytes = mBytes;

        while (true) {
            var r = await WebSocket.ReceiveAsync(mFreeBytes, cancellationToken).ConfigureAwait(false);
            switch (r.MessageType) {
            case WebSocketMessageType.Binary:
                // We skip binary messages
                continue;
            case WebSocketMessageType.Close:
                // Nothing else to do
                return;
            case WebSocketMessageType.Text:
                // Let's break from "switch" to process it
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            string? TryDecodeMessage()
            {
                var freeChars = mChars.Span;
                var readBytes = mFreeBytes.Span.Slice(0, r.Count);
                decoder.Convert(readBytes, freeChars, r.EndOfMessage, out var usedByteCount, out var usedCharCount, out var completed);
                Debug.Assert(completed);
                var readChars = freeChars.Slice(0, usedCharCount);
                var undecoded = readBytes.Slice(usedByteCount);

                if (decodedPart != null) {
                    decodedPart.Append(readChars);
                    if (r.EndOfMessage) {
                        Debug.Assert(undecoded.Length == 0);
                        var message = decodedPart.ToString();
                        decodedPart = null;
                        return message;
                    }

                    undecoded.CopyTo(mBytes.Span);
                    mFreeBytes = mBytes.Slice(undecoded.Length);
                    return null;
                }

                if (r.EndOfMessage)
                    return new string(readChars);

                decodedPart = new StringBuilder(readChars.Length);
                decodedPart.Append(readChars);
                undecoded.CopyTo(mBytes.Span);
                mFreeBytes = mBytes.Slice(undecoded.Length);
                return null;
            }

            var result = TryDecodeMessage();
            if (result != null)
                await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual async Task RunWriterUnsafe(CancellationToken cancellationToken)
    {
        using var bytesOwner = MemoryPool<byte>.Shared.Rent(WriteBufferSize);

        var encoder = Encoding.UTF8.GetEncoder();
        var reader = WriteChannel.Reader;
        var mBytes = bytesOwner.Memory;

        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out var message)) {
            var processedCount = 0;

            bool CreateMessagePart(out Memory<byte> buffer)
            {
                var remainingChars = message.AsSpan(processedCount);
                var freeBytes = mBytes.Span;
                encoder.Convert(remainingChars, freeBytes, true, out var usedCharCount, out var usedByteCount, out var completed);
                processedCount += usedCharCount;
                buffer = mBytes.Slice(0, usedByteCount);
                return completed;
            }

            while (true) {
                var isEndOfMessage = CreateMessagePart(out var buffer);
                await WebSocket
                    .SendAsync(buffer, WebSocketMessageType.Text, isEndOfMessage, cancellationToken)
                    .ConfigureAwait(false);
                if (isEndOfMessage)
                    break;
            }
        }
    }

#else

    protected virtual async Task RunReaderUnsafe(CancellationToken cancellationToken)
    {
        using var bytesOwner = ArrayPool<byte>.Shared.Lease(ReadBufferSize);
        using var charsOwner = ArrayPool<char>.Shared.Lease(ReadBufferSize);

        var decoder = Encoding.UTF8.GetDecoder();
        var writer = ReadChannel.Writer;
        var decodedPart = (StringBuilder?)null;
        var aBytes = bytesOwner.Array;
        var asBytes = new ArraySegment<byte>(aBytes);
        var mFreeBytes = asBytes;

        while (true) {
            var r = await WebSocket.ReceiveAsync(mFreeBytes, cancellationToken).ConfigureAwait(false);
            switch (r.MessageType) {
                case WebSocketMessageType.Binary:
                    // We skip binary messages
                    continue;
                case WebSocketMessageType.Close:
                    // Nothing else to do
                    return;
                case WebSocketMessageType.Text:
                    // Let's break from "switch" to process it
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            string? TryDecodeMessage()
            {
                var freeChars = new ArraySegment<char>(charsOwner.Array);
                var readBytes = mFreeBytes.Slice(0, r.Count);
                decoder.Convert(
                    readBytes.Array, readBytes.Offset, r.Count,
                    freeChars.Array, freeChars.Offset, freeChars.Count,
                    r.EndOfMessage,
                    out var usedByteCount, out var usedCharCount, out var completed);

                Debug.Assert(completed);
                var readChars = freeChars.Slice(0, usedCharCount);
                var undecoded = readBytes.Slice(usedByteCount);

                if (decodedPart != null) {
                    decodedPart.Append(readChars);
                    if (r.EndOfMessage) {
                        Debug.Assert(undecoded.Count == 0);
                        var message = decodedPart.ToString();
                        decodedPart = null;
                        return message;
                    }

                    undecoded.CopyTo(aBytes);
                    mFreeBytes = asBytes.Slice(undecoded.Count);
                    return null;
                }

                if (r.EndOfMessage)
                    return ArraySegmentCompatExt.ToString(readChars);

                decodedPart = new StringBuilder(readChars.Count);
                decodedPart.Append(readChars);
                undecoded.CopyTo(aBytes);
                mFreeBytes = asBytes.Slice(undecoded.Count);
                return null;
            }

            var result = TryDecodeMessage();
            if (result != null)
                await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual async Task RunWriterUnsafe(CancellationToken cancellationToken)
    {
        using var bytesOwner = ArrayPool<byte>.Shared.Lease(WriteBufferSize);

        var encoder = Encoding.UTF8.GetEncoder();
        var reader = WriteChannel.Reader;
        var aBytes = bytesOwner.Array;

        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out var message)) {
            var messageChars = message.ToCharArray(); // TODO: rework, very inefficient, use chars array buffer.
            var processedCount = 0;

            bool CreateMessagePart(out ArraySegment<byte> buffer)
            {
                var remainingChars = new ArraySegment<char>(messageChars, processedCount, messageChars.Length - processedCount);
                var freeBytes = new ArraySegment<byte>(aBytes);
                encoder.Convert(
                    remainingChars.Array, remainingChars.Offset, remainingChars.Count,
                    freeBytes.Array, freeBytes.Offset, freeBytes.Count,
                    true,
                    out var usedCharCount, out var usedByteCount, out var completed);
                processedCount += usedCharCount;
                buffer = new ArraySegment<byte>(freeBytes.Array, 0, usedByteCount);
                return completed;
            }

            while (true) {
                var isEndOfMessage = CreateMessagePart(out var buffer);
                await WebSocket
                    .SendAsync(buffer, WebSocketMessageType.Text, isEndOfMessage, cancellationToken)
                    .ConfigureAwait(false);
                if (isEndOfMessage)
                    break;
            }
        }

    }
#endif
}
