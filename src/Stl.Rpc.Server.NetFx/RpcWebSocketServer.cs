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

public class RpcWebSocketServer : IHasServices
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string RequestPath { get; init; } = RpcWebSocketClient.Options.Default.RequestPath;
        public string ClientIdParameterName { get; init; } = RpcWebSocketClient.Options.Default.ClientIdParameterName;
        public WebSocketChannel2<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel2<RpcMessage>.Options.Default;
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

    public HttpStatusCode HandleRequest(IOwinContext context)
    {
        // Based on https://stackoverflow.com/questions/41848095/websockets-using-owin

        var acceptToken = context.Get<WebSocketAccept>("websocket.Accept");
        if (acceptToken == null)
            return HttpStatusCode.BadRequest;

        var query = context.Request.Query;
        var clientId = query[Settings.ClientIdParameterName];
        var peerName = RpcServerPeer.FormatName(clientId);
        if (RpcHub.GetPeer(peerName) is not RpcServerPeer peer)
            return HttpStatusCode.Unauthorized;

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
        var channel = new WebSocketChannel2<RpcMessage>(Settings.WebSocketChannelOptions, webSocket, cancellationToken);

        var query = context.Request.Query;
        var clientId = query[Settings.ClientIdParameterName];
        var peerName = RpcServerPeer.FormatName(clientId);
        if (RpcHub.GetPeer(peerName) is not RpcServerPeer peer)
            return;

        peer.SetChannel(channel);
        try {
            await channel.WhenClosed.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }

    private static T? GetValue<T>(IDictionary<string, object?> env, string key)
        => env.TryGetValue(key, out var value) && value is T result ? result : default;
}
