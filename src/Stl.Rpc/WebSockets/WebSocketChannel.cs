using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance;
using Stl.Internal;
using Stl.IO;
using Stl.IO.Internal;
using Errors = Stl.Rpc.Internal.Errors;

namespace Stl.Rpc.WebSockets;

public sealed class WebSocketChannel<T> : Channel<T>
    where T : class
{
    public record Options
    {
        public static readonly Options Default = new();

        public int WriteFrameSize { get; init; } = 1450 * 3; // 1500 is the default MTU
        public int WriteBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int ReadBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int RetainedBufferSize { get; init; } = 64_000; // Any buffer is released when it hits this size
        public int MaxItemSize { get; init; } = 130_000_000; // 130 MB;
        public TimeSpan WriteDelay { get; init; } = TimeSpan.FromMilliseconds(1); // Next timer tick, actually
        public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(10);
        public DualSerializer<T> Serializer { get; init; } = new();
        public BoundedChannelOptions ReadChannelOptions { get; init; } = new(128) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        public BoundedChannelOptions WriteChannelOptions { get; init; } = new(128) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
        };
    }

    private const int MinMessageSize = 32;

    private volatile CancellationTokenSource? _stopCts;
    private readonly Channel<T> _readChannel;
    private readonly Channel<T> _writeChannel;
    private ArrayPoolBuffer<byte> _writeBuffer;
    private readonly int _writeFrameSize;
    private readonly int _writeBufferSize;
    private readonly int _releaseBufferSize;
    private readonly int _maxItemSize;
    private readonly TimeSpan _writeDelay;
    private readonly IByteSerializer<T> _byteSerializer;
    private readonly ITextSerializer<T> _textSerializer;
    private readonly WebSocketMessageType _defaultMessageType;
    // ReSharper disable once InconsistentlySynchronizedField

    public Options Settings { get; }
    public WebSocketOwner WebSocketOwner { get; }
    public WebSocket WebSocket { get; }
    public DualSerializer<T> Serializer { get; }
    public CancellationToken StopToken { get; }
    public ILogger? Log { get; }
    public ILogger? ErrorLog { get; }
    public bool OwnsWebSocketOwner { get; init; } = true;

    public Task WhenReadCompleted { get; }
    public Task WhenWriteCompleted { get; }
    public Task WhenClosed { get; }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public WebSocketChannel(
        WebSocketOwner webSocketOwner,
        CancellationToken cancellationToken = default)
        : this(Options.Default, webSocketOwner, cancellationToken)
    { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public WebSocketChannel(
        Options settings,
        WebSocketOwner webSocketOwner,
        CancellationToken cancellationToken = default)
    {
        Settings = settings;
        WebSocketOwner = webSocketOwner;
        WebSocket = webSocketOwner.WebSocket;
        Serializer = settings.Serializer;
        Log = webSocketOwner.Services.LogFor(GetType());
        ErrorLog = Log.IfEnabled(LogLevel.Error);

        _stopCts = cancellationToken.CreateLinkedTokenSource();
        StopToken = _stopCts.Token;

        _writeFrameSize = settings.WriteFrameSize;
        _writeBufferSize = settings.WriteBufferSize;
        _releaseBufferSize = settings.RetainedBufferSize;
        _maxItemSize = settings.MaxItemSize;
        _writeDelay = settings.WriteDelay.Positive();
        _byteSerializer = settings.Serializer.ByteSerializer;
        _textSerializer = settings.Serializer.TextSerializer;
        _defaultMessageType = Serializer.DefaultFormat == DataFormat.Text
            ? WebSocketMessageType.Text
            : WebSocketMessageType.Binary;
        _writeBuffer = new ArrayPoolBuffer<byte>(settings.WriteBufferSize);

        _readChannel = Channel.CreateBounded<T>(settings.ReadChannelOptions);
        _writeChannel = Channel.CreateBounded<T>(settings.WriteChannelOptions);
        Reader = _readChannel.Reader;
        Writer = _writeChannel.Writer;

        using var _ = ExecutionContextExt.TrySuppressFlow();
        WhenReadCompleted = Task.Run(() => RunReader(StopToken), default);
        WhenWriteCompleted = Task.Run(() => RunWriter(StopToken), default);
        WhenClosed = Task.Run(async () => {
            var completedTask = await Task.WhenAny(WhenReadCompleted, WhenWriteCompleted).ConfigureAwait(false);
            if (completedTask != WhenWriteCompleted)
                await WhenWriteCompleted.SilentAwait(false);
            else
                await WhenReadCompleted.SilentAwait(false);

            try {
                await completedTask.ConfigureAwait(false);
            }
            catch (Exception error) {
                await CloseWebSocket(error).ConfigureAwait(false);
                throw;
            }
            await CloseWebSocket(null).ConfigureAwait(false);
        }, default);
    }

    public async ValueTask Close()
    {
        var stopCts = Interlocked.Exchange(ref _stopCts, null);
        if (stopCts == null)
            return;

        stopCts.CancelAndDisposeSilently();
        await WhenClosed.SilentAwait(false);
        if (OwnsWebSocketOwner)
            await WebSocketOwner.DisposeAsync().ConfigureAwait(false);
        _writeBuffer.Dispose();
    }

    // Private methods

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async Task RunReader(CancellationToken cancellationToken)
    {
        var writer = _readChannel.Writer;
        try {
            await foreach (var item in ReadAll(cancellationToken).ConfigureAwait(false)) {
                while (!writer.TryWrite(item))
                    await writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false);
            }
            writer.TryComplete();
        }
        catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) {
            // This is a normal closure in most of cases,
            // so we don't want to report it as an error
            writer.TryComplete();
        }
        catch (Exception e) {
            writer.TryComplete(e);
            throw;
        }
        finally {
            _ = Close();
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async Task RunWriter(CancellationToken cancellationToken)
    {
        try {
            var reader = _writeChannel.Reader;
            if (_defaultMessageType == WebSocketMessageType.Binary) {
                // Binary -> we build frames
                if (_writeDelay != default) {
                    // There is write delay -> we use more complex write logic
                    await RunWriterWithWriteDelay(reader, cancellationToken).ConfigureAwait(false);
                    return;
                }

                // Simpler logic for no write delay case
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (reader.TryRead(out var item)) {
                        if (TrySerialize(item, _writeBuffer) && _writeBuffer.WrittenCount >= _writeFrameSize)
                            await FlushWriteBuffer(false, cancellationToken).ConfigureAwait(false);
                    }
                    await FlushWriteBuffer(true, cancellationToken).ConfigureAwait(false);
                }
            }
            else {
                // Text -> no frames
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (reader.TryRead(out var item)) {
                        if (TrySerialize(item, _writeBuffer))
                            await FlushWriteBuffer(true, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        finally {
            _ = Close();
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async Task RunWriterWithWriteDelay(ChannelReader<T> reader, CancellationToken cancellationToken)
    {
        Task? whenMustFlush = null; // null = no flush required / nothing to flush
        Task<bool>? waitToReadTask = null;
        while (true) {
            // When we are here, the sync read part is completed, so WaitToReadAsync will likely await.
            if (whenMustFlush != null) {
                if (whenMustFlush.IsCompleted) {
                    // Flush is required right now.
                    // We aren't going to check WaitToReadAsync, coz most likely it's going to await.
                    if (_writeBuffer.WrittenCount > 0)
                        await FlushWriteBuffer(true, cancellationToken).ConfigureAwait(false);
                    whenMustFlush = null;
                }
                else {
                    // Flush is pending.
                    // We must await for either it or WaitToReadAsync - what comes first.
                    waitToReadTask ??= reader.WaitToReadAsync(cancellationToken).AsTask();
                    await Task.WhenAny(whenMustFlush, waitToReadTask).ConfigureAwait(false);
                    if (!waitToReadTask.IsCompleted)
                        continue; // whenMustFlush is completed, waitToReadTask is not
                }
            }

            // If we're here, it's either:
            // - whenMustFlush == null -> we only need to await for waitToReadTask or WaitToReadAsync
            // - both whenMustFlush and waitToReadTask are completed
            bool canRead;
            if (waitToReadTask != null) {
                canRead = await waitToReadTask.ConfigureAwait(false);
                waitToReadTask = null;
            }
            else
                canRead = await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
            if (!canRead)
                break; // Reading is done

            while (reader.TryRead(out var item)) {
                if (!TrySerialize(item, _writeBuffer))
                    continue; // Nothing is written

                if (_writeBuffer.WrittenCount >= _writeFrameSize) {
                    await FlushWriteBuffer(false, cancellationToken).ConfigureAwait(false);
                    // We just "crossed" _writeFrameSize boundary, so the flush we just made
                    // flushed everything except maybe the most recent item.
                    // We can safely "declare" that if any flush was expected before that moment,
                    // it just happened. As for the most recent item, see the next "if".
                    whenMustFlush = null;
                }
            }
            if (whenMustFlush == null && _writeBuffer.WrittenCount > 0) {
                // If we're here, the write flush isn't "planned" yet + there is some data to flush.
                whenMustFlush = Task.Delay(_writeDelay, CancellationToken.None);
            }
        }
        // Final write flush
        await FlushWriteBuffer(true, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FlushWriteBuffer(bool completely, CancellationToken cancellationToken)
    {
        try {
            if (_writeBuffer.WrittenCount == 0)
                return;

            var memory = _writeBuffer.WrittenMemory;
            for (var start = 0; start < memory.Length; start += _writeFrameSize) {
                var length = Math.Min(_writeFrameSize, memory.Length - start);
                // length is always > 0 below
                var end = start + length;
                var part = memory[start..end];
                if (length < _writeFrameSize && !completely) {
                    if (start == 0)
                        return; // Nothing to copy

                    part.CopyTo(MemoryMarshal.AsMemory(memory));
                    _writeBuffer.Index = length;
                    return;
                }
                var isEndOfMessage = end == memory.Length;

                await WebSocket
                    .SendAsync(part, _defaultMessageType, isEndOfMessage, cancellationToken)
                    .ConfigureAwait(false);
            }
            _writeBuffer.Reset();
        }
        finally {
            if (_writeBuffer.WrittenCount == 0 && _writeBuffer.Capacity > _releaseBufferSize) {
                _writeBuffer.Dispose();
                _writeBuffer = new ArrayPoolBuffer<byte>(_writeBufferSize);
            }
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async IAsyncEnumerable<T> ReadAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var readBufferSize = Settings.ReadBufferSize;
        var readBuffer = ArrayPool<byte>.Shared.Rent(readBufferSize);
        var byteBuffer = new ArrayPoolBuffer<byte>();
        var textBuffer = new ArrayPoolBuffer<byte>();
        try {
            while (true) {
                T value;
                var r = await WebSocket.ReceiveAsync(readBuffer, cancellationToken).ConfigureAwait(false);
                switch (r.MessageType) {
                case WebSocketMessageType.Binary:
                    if (r.EndOfMessage && byteBuffer.WrittenCount == 0) {
                        // No-copy deserialization
                        var buffer = new ReadOnlyMemory<byte>(readBuffer, 0, r.Count);
                        while (buffer.Length != 0)
                            if (TryDeserializeBytes(ref buffer, out value))
                                yield return value;
                    }
                    else {
                        byteBuffer.Write(new ReadOnlySpan<byte>(readBuffer, 0, r.Count));
                        if (r.EndOfMessage) {
                            var buffer = byteBuffer.WrittenMemory;
                            while (buffer.Length != 0)
                                if (TryDeserializeBytes(ref buffer, out value))
                                    yield return value;

                            byteBuffer.Reset(readBufferSize, _releaseBufferSize);
                        }
                    }
                    continue;
                case WebSocketMessageType.Text:
                    if (r.EndOfMessage && textBuffer.WrittenCount == 0) {
                        // No-copy deserialization
                        if (TryDeserializeText(readBuffer, out value))
                            yield return value;
                    }
                    else {
                        textBuffer.Write(new ReadOnlySpan<byte>(readBuffer, 0, r.Count));
                        if (r.EndOfMessage) {
                            if (TryDeserializeText(textBuffer.WrittenMemory, out value))
                                yield return value;

                            textBuffer.Reset(readBufferSize, _releaseBufferSize);
                        }
                    }
                    break;
                case WebSocketMessageType.Close:
                    yield break;
                default:
                    throw Errors.UnsupportedWebSocketMessageKind();
                }
            }
        }
        finally {
            byteBuffer.Dispose();
            textBuffer.Dispose();
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    private async Task CloseWebSocket(Exception? error)
    {
        if (error is OperationCanceledException)
            error = null;

        var status = WebSocketCloseStatus.NormalClosure;
        var message = "Ok.";
        if (error != null) {
            status = WebSocketCloseStatus.InternalServerError;
            message = "Internal Server Error.";
            ErrorLog?.LogError(error, "WebSocket is closing after an error");
        }

        try {
            await WebSocket.CloseAsync(status, message, default)
                .WaitAsync(Settings.CloseTimeout, CancellationToken.None)
                .SilentAwait(false);
        }
        catch {
            // Intended
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private bool TrySerialize(T value, ArrayPoolBuffer<byte> buffer)
    {
        var startOffset = buffer.WrittenCount;
        try {
            int size;
            if (_defaultMessageType == WebSocketMessageType.Text) {
                _textSerializer.Write(buffer, value);
                size = buffer.WrittenCount - startOffset;
                if (size > _maxItemSize)
                    throw Errors.ItemSizeExceedsTheLimit();
            }
            else {
                buffer.GetSpan(MinMessageSize);
                buffer.Advance(4);
                _byteSerializer.Write(buffer, value);
                size = buffer.WrittenCount - startOffset;
                buffer.WrittenSpan.WriteUnchecked(startOffset, size);
                if (size > _maxItemSize)
                    throw Errors.ItemSizeExceedsTheLimit();

                // Log?.LogInformation("Wrote: {Value}", value);
                // Log?.LogInformation("Data({Size}): {Data}",
                //     size - 4, new Base64Encoded(buffer.WrittenMemory[(startOffset + 4)..].ToArray()).Encode());
            }
            return true;
        }
        catch (Exception e) {
            buffer.Index = startOffset;
            ErrorLog?.LogError(e,
                "Couldn't serialize the value of type '{Type}'",
                value?.GetType().FullName ?? "null");
            return false;
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private bool TryDeserializeBytes(ref ReadOnlyMemory<byte> bytes, out T value)
    {
        int size = 0;
        bool isSizeValid = false;
        try {
            size = bytes.Span.ReadUnchecked<int>();
            isSizeValid = size > 0 && size <= bytes.Length;
            if (!isSizeValid)
                throw Errors.InvalidItemSize();
            if (size > _maxItemSize)
                throw Errors.ItemSizeExceedsTheLimit();

            var data = bytes[sizeof(int)..size];
            value = _byteSerializer.Read(data, out int readSize);
            if (readSize != size - 4)
                throw Errors.InvalidItemSize();

            // Log?.LogInformation("Read: {Value}", value);
            // Log?.LogInformation("Data({Size}): {Data}",
            //     readSize, new Base64Encoded(data.ToArray()).Encode());

            bytes = bytes[size..];
            return true;
        }
        catch (Exception e) {
            ErrorLog?.LogError(e, "Couldn't deserialize: {Data}", new TextOrBytes(DataFormat.Bytes, bytes));
            value = default!;
            bytes = isSizeValid ? bytes[size..] : default;
            return false;
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private bool TryDeserializeText(ReadOnlyMemory<byte> bytes, out T value)
    {
        try {
            if (bytes.Length > _maxItemSize)
                throw Errors.ItemSizeExceedsTheLimit();

            value = _textSerializer.Read(bytes);
            return true;
        }
        catch (Exception e) {
            ErrorLog?.LogError(e, "Couldn't deserialize: {Data}", new TextOrBytes(DataFormat.Text, bytes));
            value = default!;
            return false;
        }
    }
}
