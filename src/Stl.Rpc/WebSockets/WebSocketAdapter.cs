using System.Buffers;
using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.IO;

namespace Stl.Rpc.WebSockets;

public sealed class WebSocketAdapter<T>
{
    public record Options
    {
        public static Options Default { get; } = new();

        public bool OwnsWebSocket { get; init; } = true;
        public int WriteBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int ReadBufferSize { get; init; } = 16_000; // Rented ~just once, so it can be large
        public int ReleaseBufferSize { get; init; } = 64_000; // Any buffer is released when it hits this size
        public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
        public DualSerializer<T> Serializer { get; init; } = new();
        public ILogger? Log { get; init; }
    }

    private ArrayPoolBufferWriter<byte> _writeBufferWriter;
    private readonly int _releaseBufferSize;

    public Options Settings { get; }
    public WebSocket WebSocket { get; }
    public DualSerializer<T> Serializer { get; }
    public ILogger? Log { get; }

    public WebSocketAdapter(WebSocket webSocket) : this(Options.Default, webSocket) { }
    public WebSocketAdapter(Options settings, WebSocket webSocket)
    {
        Settings = settings;
        WebSocket = webSocket;
        Serializer = settings.Serializer;
        Log = settings.Log;
        _writeBufferWriter = new ArrayPoolBufferWriter<byte>(settings.WriteBufferSize);
        _releaseBufferSize = Settings.ReleaseBufferSize;
    }

    public async ValueTask Write(T value, CancellationToken cancellationToken = default)
    {
        if (!TrySerialize(value, _writeBufferWriter))
            return;

        try {
            var messageType = Serializer.DefaultFormat == DataFormat.Text
                ? WebSocketMessageType.Text
                : WebSocketMessageType.Binary;
            await WebSocket
                .SendAsync(_writeBufferWriter, messageType, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally {
            RenewBuffer(ref _writeBufferWriter, Settings.WriteBufferSize);
        }
    }

    public async IAsyncEnumerable<T> ReadAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Exception? error = null;
        var readBufferSize = Settings.ReadBufferSize;
        var buffer = ArrayPool<byte>.Shared.Rent(readBufferSize);
        var byteBufferWriter = new ArrayPoolBufferWriter<byte>();
        var textBufferWriter = new ArrayPoolBufferWriter<byte>();
        try {
            while (true) {
                T value;
                var r = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                switch (r.MessageType) {
                case WebSocketMessageType.Binary:
                    if (r.EndOfMessage && byteBufferWriter.WrittenCount == 0) {
                        // No-copy deserialization
                        if (TryDeserialize(buffer, DataFormat.Bytes, out value))
                            yield return value;
                    }
                    else {
                        byteBufferWriter.Write(buffer.AsSpan(0, r.Count));
                        if (r.EndOfMessage) {
                            if (TryDeserialize(byteBufferWriter.WrittenMemory, DataFormat.Bytes, out value))
                                yield return value;

                            RenewBuffer(ref byteBufferWriter, readBufferSize);
                        }
                    }
                    continue;
                case WebSocketMessageType.Text:
                    if (r.EndOfMessage && textBufferWriter.WrittenCount == 0) {
                        // No-copy deserialization
                        if (TryDeserialize(buffer, DataFormat.Text, out value))
                            yield return value;
                    }
                    else {
                        textBufferWriter.Write(buffer.AsSpan(0, r.Count));
                        if (r.EndOfMessage) {
                            if (TryDeserialize(textBufferWriter.WrittenMemory, DataFormat.Text, out value))
                                yield return value;

                            RenewBuffer(ref textBufferWriter, readBufferSize);
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
            byteBufferWriter.Dispose();
            textBufferWriter.Dispose();
            ArrayPool<byte>.Shared.Return(buffer);
            await Close(error).ConfigureAwait(false);
        }
    }

    public async Task Close(Exception? error = null)
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
        finally {
            if (Settings.OwnsWebSocket)
                WebSocket.Dispose();
        }
    }

    // Private methods

    private bool TrySerialize(T value, IBufferWriter<byte> bufferWriter)
    {
        try {
            Serializer.Write(value, Serializer.DefaultFormat, bufferWriter);
            return true;
        }
        catch (Exception e) {
            Log?.LogError(e, "Couldn't serialize value of type '{Type}'", value?.GetType());
            return false;
        }
    }

    private bool TryDeserialize(ReadOnlyMemory<byte> bytes, DataFormat format, out T value)
    {
        try {
            value = Serializer.Read(bytes, format);
            return true;
        }
        catch (Exception e) {
            Log?.LogError(e, "Couldn't deserialize: {Data}", new TextOrBytes(format, bytes));
            value = default!;
            return false;
        }
    }

    private void RenewBuffer(ref ArrayPoolBufferWriter<byte> bufferWriter, int capacity)
    {
        if (bufferWriter.Capacity <= _releaseBufferSize)
            bufferWriter.Reset();
        else {
            bufferWriter.Dispose();
            bufferWriter = new ArrayPoolBufferWriter<byte>(capacity);
        }
    }
}
