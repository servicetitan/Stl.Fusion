using System.Buffers;
using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.IO;
using Stl.Rpc.Internal;

namespace Stl.Rpc.WebSockets;

public sealed class WebSocketChannel<T> : Channel<T>
    where T : class
{
    public record Options
    {
        public static Options Default { get; } = new();

        public bool OwnsWebSocket { get; init; } = true;
        public int WritePacketSize { get; init; } = 1400;
        public int WriteBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int ReadBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int RetainedBufferSize { get; init; } = 64_000; // Any buffer is released when it hits this size
        public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(10);
        public DualSerializer<T> Serializer { get; init; } = new();
        public BoundedChannelOptions ReadChannelOptions { get; init; } = new(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        };
        public BoundedChannelOptions WriteChannelOptions { get; init; } = new(16) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
        };
        public ILogger? Log { get; init; }
    }

    private volatile CancellationTokenSource? _stopCts;
    private readonly Channel<T> _readChannel;
    private readonly Channel<T> _writeChannel;
    private ArrayPoolBufferWriter<byte> _writeBuffer;
    private readonly int _writePacketSize;
    private readonly int _writeBufferSize;
    private readonly int _releaseBufferSize;
    private readonly IByteSerializer<T> _byteSerializer;
    private readonly ITextSerializer<T> _textSerializer;
    private readonly WebSocketMessageType _defaultMessageType;
    // ReSharper disable once InconsistentlySynchronizedField

    public Options Settings { get; }
    public WebSocket WebSocket { get; }
    public DualSerializer<T> Serializer { get; }
    public CancellationToken StopToken { get; }
    public ILogger? Log { get; }

    public Task WhenReadCompleted { get; }
    public Task WhenWriteCompleted { get; }
    public Task WhenClosed { get; }

    public WebSocketChannel(WebSocket webSocket, CancellationToken cancellationToken = default)
        : this(Options.Default, webSocket, cancellationToken) { }
    public WebSocketChannel(Options settings, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        WebSocket = webSocket;
        Serializer = settings.Serializer;
        Log = settings.Log;
        _stopCts = cancellationToken.CreateLinkedTokenSource();
        StopToken = _stopCts.Token;

        _writePacketSize = settings.WritePacketSize;
        _writeBufferSize = settings.WriteBufferSize;
        _releaseBufferSize = settings.RetainedBufferSize;
        _byteSerializer = settings.Serializer.ByteSerializer;
        _textSerializer = settings.Serializer.TextSerializer;
        _defaultMessageType = Serializer.DefaultFormat == DataFormat.Text
            ? WebSocketMessageType.Text
            : WebSocketMessageType.Binary;
        _writeBuffer = new ArrayPoolBufferWriter<byte>(settings.WriteBufferSize);

        _readChannel = Channel.CreateBounded<T>(settings.ReadChannelOptions);
        _writeChannel = Channel.CreateBounded<T>(settings.WriteChannelOptions);
        Reader = _readChannel.Reader;
        Writer = _writeChannel.Writer;

        using var _ = ExecutionContextExt.SuppressFlow();

        WhenReadCompleted = Task.Run(() => RunReader(StopToken), default);
        WhenWriteCompleted = Task.Run(() => RunWriter(StopToken), default);
        WhenClosed = Task.Run(async () => {
            var firstCompletedTask = await Task.WhenAny(WhenReadCompleted, WhenWriteCompleted).ConfigureAwait(false);
            if (firstCompletedTask != WhenWriteCompleted)
                await WhenWriteCompleted.SilentAwait(false);
            else
                await WhenReadCompleted.SilentAwait(false);

            try {
                await firstCompletedTask.ConfigureAwait(false);
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
        if (Settings.OwnsWebSocket)
            WebSocket.Dispose();
    }

    // Private methods

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

    private async Task RunWriter(CancellationToken cancellationToken)
    {
        try {
            var reader = _writeChannel.Reader;
            if (_defaultMessageType == WebSocketMessageType.Binary) {
                // Binary -> we build frames
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (reader.TryRead(out var item)) {
                        if (TrySerialize(item, _writeBuffer) && _writeBuffer.WrittenCount >= _writePacketSize)
                            await FlushWriteBuffer(false, cancellationToken).ConfigureAwait(false);
                    }
                    if (_writeBuffer.WrittenCount > 0)
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

    private async ValueTask FlushWriteBuffer(bool completely, CancellationToken cancellationToken)
    {
        try {
            if (_writeBuffer.WrittenCount == 0)
                return;

            var memory = _writeBuffer.WrittenMemory;
            for (var start = 0; start < memory.Length; start += _writePacketSize) {
                var length = Math.Min(_writePacketSize, memory.Length - start);
                // length is always > 0 below
                var end = start + length;
                var part = memory[start..end];
                if (length < _writePacketSize && !completely) {
                    if (start == 0)
                        return; // Nothing to copy

                    part.CopyTo(MemoryMarshal.AsMemory(memory));
                    _writeBuffer.Reset(length);
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
                _writeBuffer = new ArrayPoolBufferWriter<byte>(_writeBufferSize);
            }
        }
    }

    private async IAsyncEnumerable<T> ReadAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var readBufferSize = Settings.ReadBufferSize;
        var readBuffer = ArrayPool<byte>.Shared.Rent(readBufferSize);
        var byteBuffer = new ArrayPoolBufferWriter<byte>();
        var textBuffer = new ArrayPoolBufferWriter<byte>();
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

                            RenewBuffer(ref byteBuffer, readBufferSize);
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

                            RenewBuffer(ref textBuffer, readBufferSize);
                        }
                    }
                    break;
                case WebSocketMessageType.Close:
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException();
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

    private bool TrySerialize(T value, ArrayPoolBufferWriter<byte> buffer)
    {
        var startOffset = buffer.WrittenCount;
        try {
            if (_defaultMessageType == WebSocketMessageType.Text)
                _textSerializer.Write(buffer, value);
            else {
                var sizeSpan = buffer.GetSpan(4).Cast<byte, int>();
                buffer.Advance(4);
                _byteSerializer.Write(buffer, value);
                sizeSpan[0] = buffer.WrittenCount - startOffset;
            }
            return true;
        }
        catch (Exception e) {
            buffer.Reset(startOffset);
            Log?.LogError(e, "Couldn't serialize value of type '{Type}'", value?.GetType());
            return false;
        }
    }

    private bool TryDeserializeBytes(ref ReadOnlyMemory<byte> bytes, out T value)
    {
        int size = 0;
        bool isSizeValid = false;
        try {
            size = bytes.Span[..4].Cast<byte, int>()[0];
            isSizeValid = size > 0 && size <= bytes.Length;
            if (!isSizeValid)
                throw Errors.InvalidMessageSize();

            value = _byteSerializer.Read(bytes[4..], out int _);
            bytes = bytes[size..];
            return true;
        }
        catch (Exception e) {
            Log?.LogError(e, "Couldn't deserialize: {Data}", new TextOrBytes(DataFormat.Bytes, bytes));
            value = default!;
            bytes = isSizeValid ? bytes[size..] : default;
            return false;
        }
    }

    private bool TryDeserializeText(ReadOnlyMemory<byte> bytes, out T value)
    {
        try {
            value = _textSerializer.Read(bytes);
            return true;
        }
        catch (Exception e) {
            Log?.LogError(e, "Couldn't deserialize: {Data}", new TextOrBytes(DataFormat.Text, bytes));
            value = default!;
            return false;
        }
    }

    private void RenewBuffer(ref ArrayPoolBufferWriter<byte> buffer, int capacity)
    {
        if (buffer.Capacity <= _releaseBufferSize)
            buffer.Reset();
        else {
            buffer.Dispose();
            buffer = new ArrayPoolBufferWriter<byte>(capacity);
        }
    }
}
