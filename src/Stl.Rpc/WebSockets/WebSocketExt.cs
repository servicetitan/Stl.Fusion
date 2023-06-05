using System.Net.WebSockets;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.Rpc.WebSockets;

public static class WebSocketExt
{
    public static ValueTask SendAsync(this WebSocket webSocket,
        IBuffer<byte> bufferWriter,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        var segment = new ArraySegment<byte>(bufferWriter.WrittenMemory.ToArray());
        return webSocket.SendAsync(segment, messageType, endOfMessage, cancellationToken).ToValueTask();
#else
        return webSocket.SendAsync(bufferWriter.WrittenMemory, messageType, endOfMessage, cancellationToken);
#endif
    }

#if NETSTANDARD2_0
    public static Task<WebSocketReceiveResult> ReceiveAsync(this WebSocket webSocket,
        byte[] buffer, CancellationToken cancellationToken)
        => webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
#else
    public static ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(this WebSocket webSocket,
        byte[] buffer, CancellationToken cancellationToken)
        => webSocket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
#endif
}
