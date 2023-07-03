using System.Net;
using System.Net.WebSockets;
using Microsoft.Owin;
using Stl.Rpc.Clients;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;
using WebSocketAccept = System.Action<
    System.Collections.Generic.IDictionary<string, object>, // WebSocket Accept parameters
    System.Func< // WebSocketFunc callback
        System.Collections.Generic.IDictionary<string, object>, // WebSocket environment
        System.Threading.Tasks.Task>>;

namespace Stl.Rpc.Server;

public class RpcWebSocketServer : RpcServiceBase
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string RoutePattern { get; init; } = RpcWebSocketClient.Options.Default.RequestPath;
        public string ClientIdParameterName { get; init; } = RpcWebSocketClient.Options.Default.ClientIdParameterName;
        public WebSocketChannel<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel<RpcMessage>.Options.Default;
    }

    public Options Settings { get; }
    public RpcWebSocketServerPeerRefFactory PeerRefFactory { get; }

    public RpcWebSocketServer(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        PeerRefFactory = services.GetRequiredService<RpcWebSocketServerPeerRefFactory>();
    }

    public HttpStatusCode Invoke(IOwinContext context)
    {
        // Based on https://stackoverflow.com/questions/41848095/websockets-using-owin

        var acceptToken = context.Get<WebSocketAccept>("websocket.Accept");
        if (acceptToken == null)
            return HttpStatusCode.BadRequest;

        var peerRef = PeerRefFactory.Invoke(this, context);
        if (Hub.GetPeer(peerRef) is not RpcServerPeer)
            return HttpStatusCode.NotFound;

        var requestHeaders =
            GetValue<IDictionary<string, string[]>>(context.Environment, "owin.RequestHeaders")
            ?? ImmutableDictionary<string, string[]>.Empty;

        var acceptOptions = new Dictionary<string, object>(StringComparer.Ordinal);
        if (requestHeaders.TryGetValue("Sec-WebSocket-Protocol", out string[]? subProtocols) && subProtocols.Length > 0) {
            // Select the first one from the client
            acceptOptions.Add("websocket.SubProtocol", subProtocols[0].Split(',').First().Trim());
        }

        acceptToken(acceptOptions, wsEnv => {
            var wsContext = (WebSocketContext)wsEnv["System.Net.WebSockets.WebSocketContext"];
            return HandleWebSocket(context, wsContext);
        });

        return HttpStatusCode.SwitchingProtocols;
    }

    private async Task HandleWebSocket(IOwinContext context, WebSocketContext wsContext)
    {
        var cancellationToken = context.Request.CallCancelled;
        var webSocket = wsContext.WebSocket;
        var channel = new WebSocketChannel<RpcMessage>(
            Settings.WebSocketChannelOptions, webSocket, Services, cancellationToken);

        var peerRef = PeerRefFactory.Invoke(this, context);
        if (Hub.GetPeer(peerRef) is not RpcServerPeer peer)
            return;

        var options = ImmutableOptionSet.Empty.Set(context).Set(webSocket);
        var connection = new RpcConnection(channel, options);
        try {
            await peer.Connect(connection, cancellationToken).ConfigureAwait(false);
            await channel.WhenClosed.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }

    private static T? GetValue<T>(IDictionary<string, object?> env, string key)
        => env.TryGetValue(key, out var value) && value is T result ? result : default;
}
