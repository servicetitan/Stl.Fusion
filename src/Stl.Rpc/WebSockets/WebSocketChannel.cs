using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Text;

#if NETSTANDARD2_0
using Stl.Pooling;
#endif

namespace Stl.Rpc.WebSockets;

public class WebSocketChannel : Channel<string>, IAsyncDisposable
{
    public record Options
    {
        public static Options Default { get; } = new();

        public bool OwnsWebSocket { get; init; } = true;
        public int ReadBufferSize { get; init; } = 16_384;
        public int WriteBufferSize { get; init; } = 16_384;
        public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
        public Func<Channel<string>> ReadChannelFactory { get; init; }
        public Func<Channel<string>> WriteChannelFactory { get; init; }
        public BoundedChannelOptions ChannelOptions { get; init; } = new(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };

        public Options()
        {
            ReadChannelFactory = DefaultChannelFactory;
            WriteChannelFactory = DefaultChannelFactory;
        }

        public Channel<string> DefaultChannelFactory()
            => Channel.CreateBounded<string>(ChannelOptions);
    }

    private readonly Task _whenReadCompletedTask;
    private readonly Task _whenWriteCompletedTask;
    private readonly Task _whenClosedTask;

    protected volatile CancellationTokenSource? StopCts;

    protected Channel<string> ReadChannel { get; }
    protected Channel<string> WriteChannel { get; }

    public Options Settings { get; }
    public WebSocket WebSocket { get; }
    public CancellationToken StopToken { get; }

    public WebSocketChannel(WebSocket webSocket) : this(Options.Default, webSocket) { }
    public WebSocketChannel(Options settings, WebSocket webSocket)
    {
        Settings = settings;
        WebSocket = webSocket;
        ReadChannel = Settings.ReadChannelFactory.Invoke();
        WriteChannel = Settings.WriteChannelFactory.Invoke();
        Reader = ReadChannel.Reader;
        Writer = WriteChannel.Writer;

        StopCts = new CancellationTokenSource();
        StopToken = StopCts.Token;

        using var _ = ExecutionContextExt.SuppressFlow();
        _whenReadCompletedTask = Task.Run(() => RunReader(StopToken), default);
        _whenWriteCompletedTask = Task.Run(() => RunWriter(StopToken), default);
        _whenClosedTask = Task.Run(async () => {
            var readError = await _whenReadCompletedTask.WaitErrorAsync().ConfigureAwait(false);
            var writeError = await _whenWriteCompletedTask.WaitErrorAsync().ConfigureAwait(false);
            var error = readError ?? writeError;
            await Close(error).ConfigureAwait(false);
            if (readError != null)
                ExceptionDispatchInfo.Capture(readError).Throw();
        }, default);
    }

    public async ValueTask DisposeAsync()
    {
        var stopCts = Interlocked.Exchange(ref StopCts, null!);
        if (stopCts == null)
            return;

        stopCts.CancelAndDisposeSilently();
        try {
            await _whenClosedTask.ConfigureAwait(false);
        }
        catch {
            // Dispose shouldn't throw exceptions
        }
        if (Settings.OwnsWebSocket)
            WebSocket.Dispose();
    }

    public Task WhenReadCompleted() => _whenReadCompletedTask;
    public Task WhenWriteCompleted() => _whenWriteCompletedTask;
    public Task WhenClosed() => _whenClosedTask;

    // Protected methods

    protected virtual async Task Close(Exception? error = null)
    {
        if (error is OperationCanceledException)
            error = null;

        var status = WebSocketCloseStatus.NormalClosure;
        var message = "Ok.";
        if (error != null) {
            status = WebSocketCloseStatus.InternalServerError;
            message = "Internal Server Error.";
        }

        using var cts = new CancellationTokenSource(Settings.CloseTimeout);
        try {
            await WebSocket.CloseAsync(status, message, cts.Token).ConfigureAwait(false);
        }
        catch {
            // Intended
        }
    }

    protected async Task RunReader(CancellationToken cancellationToken)
    {
        try {
            await RunReaderUnsafe(cancellationToken).ConfigureAwait(false);
            ReadChannel.Writer.TryComplete();
        }
        catch (Exception e) {
            ReadChannel.Writer.TryComplete(e);
            throw;
        }
    }

    protected Task RunWriter(CancellationToken cancellationToken)
        => RunWriterUnsafe(cancellationToken);

#if !NETSTANDARD2_0

    protected virtual async Task RunReaderUnsafe(CancellationToken cancellationToken)
    {
        using var bytesOwner = MemoryPool<byte>.Shared.Rent(Settings.ReadBufferSize);
        using var charsOwner = MemoryPool<char>.Shared.Rent(Settings.ReadBufferSize);

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
        using var bytesOwner = MemoryPool<byte>.Shared.Rent(Settings.WriteBufferSize);

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
        using var bytesOwner = ArrayPool<byte>.Shared.Lease(Settings.ReadBufferSize);
        using var charsOwner = ArrayPool<char>.Shared.Lease(Settings.ReadBufferSize);

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
                    decodedPart.Append(readChars.Array, readChars.Offset, readChars.Count);
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
                decodedPart.Append(readChars.Array, readChars.Offset, readChars.Count);
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
        using var bytesOwner = ArrayPool<byte>.Shared.Lease(Settings.WriteBufferSize);

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
