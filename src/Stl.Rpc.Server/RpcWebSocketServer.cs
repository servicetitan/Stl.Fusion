using System.Net;
using Microsoft.AspNetCore.Http;
using Stl.Rpc.Clients;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc.Server;

public class RpcWebSocketServer : IHasServices
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string RequestPath { get; init; } = RpcWebSocketClient.Options.Default.RequestPath;
        public string ClientIdParameterName { get; init; } = RpcWebSocketClient.Options.Default.ClientIdParameterName;
        public WebSocketChannel2<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel2<RpcMessage>.Options.Default;
#if NET6_0_OR_GREATER
        public Func<WebSocketAcceptContext> ConfigureWebSocket { get; init; } = () => new();
#endif
    }

    private ILogger? _log;
    private RpcHub? _rpcHub;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public RpcHub RpcHub => _rpcHub ??= Services.GetRequiredService<RpcHub>();

    public RpcWebSocketServer(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
    }

    public async Task HandleRequest(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        if (!context.WebSockets.IsWebSocketRequest) {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        var query = context.Request.Query;
        var clientId = query[Settings.ClientIdParameterName].SingleOrDefault() ?? "";
        var peerName = RpcServerPeer.FormatName(clientId);
        if (RpcHub.GetPeer(peerName) is not RpcServerPeer peer) {
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
        var channel = new WebSocketChannel2<RpcMessage>(Settings.WebSocketChannelOptions, webSocket, cancellationToken);

        peer.SetChannel(channel);
        try {
            await channel.WhenClosed.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
