using System.Buffers;
using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance;
using Stl.IO;
using Stl.IO.Internal;
using Stl.Rpc.Internal;

namespace Stl.Rpc.WebSockets;

public sealed class WebSocketChannel<T> : Channel<T>
    where T : class
{
    public record Options
    {
        public static Options Default { get; } = new();

        public bool OwnsWebSocket { get; init; } = true;
        public int WriteFrameSize { get; init; } = 4400;
        public int WriteBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int ReadBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int RetainedBufferSize { get; init; } = 64_000; // Any buffer is released when it hits this size
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
    private readonly IByteSerializer<T> _byteSerializer;
    private readonly ITextSerializer<T> _textSerializer;
    private readonly WebSocketMessageType _defaultMessageType;
    // ReSharper disable once InconsistentlySynchronizedField

    public Options Settings { get; }
    public WebSocket WebSocket { get; }
    public DualSerializer<T> Serializer { get; }
    public CancellationToken StopToken { get; }
    public ILogger? Log { get; }
    public ILogger? ErrorLog { get; }

    public Task WhenReadCompleted { get; }
    public Task WhenWriteCompleted { get; }
    public Task WhenClosed { get; }

    public WebSocketChannel(
        WebSocket webSocket,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
        : this(Options.Default, webSocket, services, cancellationToken)
    { }

    public WebSocketChannel(
        Options settings,
        WebSocket webSocket,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        Settings = settings;
        WebSocket = webSocket;
        Serializer = settings.Serializer;
        Log = services.LogFor(GetType());
        ErrorLog = Log.IfEnabled(LogLevel.Error);

        _stopCts = cancellationToken.CreateLinkedTokenSource();
        StopToken = _stopCts.Token;

        _writeFrameSize = settings.WriteFrameSize;
        _writeBufferSize = settings.WriteBufferSize;
        _releaseBufferSize = settings.RetainedBufferSize;
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
        _writeBuffer.Dispose();
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
                        if (TrySerialize(item, _writeBuffer) && _writeBuffer.WrittenCount >= _writeFrameSize)
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

    private bool TrySerialize(T value, ArrayPoolBuffer<byte> buffer)
    {
        var startOffset = buffer.WrittenCount;
        try {
            if (_defaultMessageType == WebSocketMessageType.Text)
                _textSerializer.Write(buffer, value);
            else {
                buffer.GetSpan(MinMessageSize);
                buffer.Advance(4);
                _byteSerializer.Write(buffer, value);
                buffer.WrittenSpan.WriteUnchecked(startOffset, buffer.WrittenCount - startOffset);

                // Log?.LogInformation("Wrote: {Value}", value);
                // Log?.LogInformation("Data({Size}): {Data}",
                //     size - 4, new Base64Encoded(buffer.WrittenMemory[(startOffset + 4)..].ToArray()).Encode());
            }
            return true;
        }
        catch (Exception e) {
            buffer.Index = startOffset;
            ErrorLog?.LogError(e, "Couldn't serialize the value of type '{Type}'", value?.GetType().FullName ?? "null");
            return false;
        }
    }

    private bool TryDeserializeBytes(ref ReadOnlyMemory<byte> bytes, out T value)
    {
        int size = 0;
        bool isSizeValid = false;
        try {
            size = bytes.Span.ReadUnchecked<int>();
            isSizeValid = size > 0 && size <= bytes.Length;
            if (!isSizeValid)
                throw Errors.InvalidMessageSize();

            var data = bytes[sizeof(int)..size];
            value = _byteSerializer.Read(data, out int readSize);
            if (readSize != size - 4)
                throw Errors.InvalidMessageSize();

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

    private bool TryDeserializeText(ReadOnlyMemory<byte> bytes, out T value)
    {
        try {
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
