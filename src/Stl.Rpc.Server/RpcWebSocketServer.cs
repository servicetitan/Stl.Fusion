using System.Net;
using Microsoft.AspNetCore.Http;
using Stl.Rpc.Clients;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc.Server;

public class RpcWebSocketServer : RpcServiceBase
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string RequestPath { get; init; } = RpcWebSocketClient.Options.Default.RequestPath;
        public string ClientIdParameterName { get; init; } = RpcWebSocketClient.Options.Default.ClientIdParameterName;
        public WebSocketChannel<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel<RpcMessage>.Options.Default;
#if NET6_0_OR_GREATER
        public Func<WebSocketAcceptContext> ConfigureWebSocket { get; init; } = () => new();
#endif
    }

    public Options Settings { get; }
    public RpcWebSocketServerPeerRefFactory PeerRefFactory { get; }

    public RpcWebSocketServer(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        PeerRefFactory = services.GetRequiredService<RpcWebSocketServerPeerRefFactory>();
    }

    public async Task HandleRequest(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        if (!context.WebSockets.IsWebSocketRequest) {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        var peerRef = PeerRefFactory.Invoke(this, context);
        if (Hub.GetPeer(peerRef) is not RpcServerPeer peer) {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

#if NET6_0_OR_GREATER
        var webSocketAcceptContext = Settings.ConfigureWebSocket.Invoke();
        var acceptWebSocketTask = context.WebSockets.AcceptWebSocketAsync(webSocketAcceptContext);
#else
        var acceptWebSocketTask = context.WebSockets.AcceptWebSocketAsync();
#endif
        var webSocket = await acceptWebSocketTask.ConfigureAwait(false);
        var channel = new WebSocketChannel<RpcMessage>(
            Settings.WebSocketChannelOptions, webSocket, Services, cancellationToken);

        peer.SetConnectionState(channel);
        try {
            await channel.WhenClosed.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
