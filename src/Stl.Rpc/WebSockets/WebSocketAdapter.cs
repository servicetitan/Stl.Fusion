using System.Buffers;
using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.Rpc.WebSockets;

public sealed class WebSocketAdapter<T>
{
    public record Options
    {
        public static Options Default { get; } = new();

        public bool OwnsWebSocket { get; init; } = true;
        public int WriteBufferSize { get; init; } = 4_000; // Rented on per-write basis
        public int ReadBufferSize { get; init; } = 16_000; // Rented just once, so it can be large
        public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
        public DualSerializer<T> Serializer { get; init; } = new();
        public ILogger? Log { get; init; }
    }

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
    }

    public ValueTask Write(T value, CancellationToken cancellationToken = default)
    {
        var sendTask = ValueTaskExt.CompletedTask;
        var bufferWriter = new ArrayPoolBufferWriter<byte>(Settings.WriteBufferSize);
        try {
            if (TrySerialize(value, bufferWriter)) {
                var messageType = Serializer.DefaultFormat == DataFormat.Text
                    ? WebSocketMessageType.Text
                    : WebSocketMessageType.Binary;
                sendTask = WebSocket.SendAsync(bufferWriter, messageType, true, cancellationToken);
            }
            return sendTask;
        }
        finally {
            if (sendTask.IsCompleted)
                bufferWriter.Dispose();
            else
                _ = sendTask.AsTask().ContinueWith(
                    _ => bufferWriter.Dispose(),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    public async IAsyncEnumerable<T> ReadAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Exception? error = null;
        var buffer = ArrayPool<byte>.Shared.Rent(Settings.ReadBufferSize);
        try {
            using var byteBufferWriter = new ArrayPoolBufferWriter<byte>();
            using var textBufferWriter = new ArrayPoolBufferWriter<byte>();
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
                            byteBufferWriter.Clear();
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
                            textBufferWriter.Clear();
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
}
