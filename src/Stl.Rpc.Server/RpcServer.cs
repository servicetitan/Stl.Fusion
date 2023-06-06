using System.Net;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc.Server;

public class RpcServer
{
    public record Options
    {
        public string RequestPath { get; init; } = "/rpc/ws";
        public string ClientIdParameterName { get; init; } = "clientId";
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

    public RpcServer(Options settings, IServiceProvider services)
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

        var requestQuery = context.Request.Query;
        var clientId = requestQuery[Settings.ClientIdParameterName].SingleOrDefault() ?? "";
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
        var wsAdapter = new WebSocketAdapter<RpcMessage>(webSocket);
        var channel = wsAdapter.ToChannel(cancellationToken);
        peer.SetChannel(channel);
        try {
            await channel.Reader.Completion.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
