using System.Net.WebSockets;

namespace Stl.Rpc.WebSockets;

public static class WebSocketExt
{
#if NETSTANDARD2_0
    public static ValueTask SendAsync(this WebSocket webSocket,
        ReadOnlyMemory<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken)
    {
        var segment = new ArraySegment<byte>(buffer.ToArray());
        return webSocket.SendAsync(segment, messageType, endOfMessage, cancellationToken).ToValueTask();
    }
#endif

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
