using System.Net;
using System.Net.WebSockets;
using Microsoft.Owin;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;

using WebSocketAccept = System.Action<
    System.Collections.Generic.IDictionary<string, object>, // WebSocket Accept parameters
    System.Func< // WebSocketFunc callback
        System.Collections.Generic.IDictionary<string, object>, // WebSocket environment
        System.Threading.Tasks.Task>>;

namespace Stl.Fusion.Server;

public class WebSocketServer : IHasServices
{
    public record Options
    {
        public string RequestPath { get; init; } = "/fusion/ws";
        public string PublisherIdQueryParameterName { get; init; } = "publisherId";
        public string ClientIdQueryParameterName { get; init; } = "clientId";
        public Func<ITextSerializer<BridgeMessage>> SerializerFactory { get; init; } =
            DefaultSerializerFactory;

        public static ITextSerializer<BridgeMessage> DefaultSerializerFactory()
            => TextSerializer.NewAsymmetric(
                new TypeDecoratingSerializer(
                    SystemJsonSerializer.Default,
                    t => typeof(ReplicatorRequest).IsAssignableFrom(t)),
                new TypeDecoratingSerializer(
                    SystemJsonSerializer.Default,
                    t => typeof(PublisherReply).IsAssignableFrom(t)
            )).ToTyped<BridgeMessage>();
    }

    protected IPublisher Publisher { get; }
    protected ILogger Log { get; }

    public Options Settings { get; }
    public IServiceProvider Services { get; }

    public WebSocketServer(Options settings, IPublisher publisher)
    {
        Settings = settings;
        Publisher = publisher;
        Services = Publisher.Services;
        Log = Services.LogFor(GetType());
    }

    public HttpStatusCode HandleRequest(IOwinContext owinContext)
    {
        // written based on https://stackoverflow.com/questions/41848095/websockets-using-owin

        var acceptToken = owinContext.Get<WebSocketAccept>("websocket.Accept");
        if (acceptToken == null)
            return HttpStatusCode.BadRequest;

        var publisherId = owinContext.Request.Query[Settings.PublisherIdQueryParameterName];
        if (Publisher.Id != publisherId)
            return HttpStatusCode.BadRequest;

        var clientId = owinContext.Request.Query[Settings.ClientIdQueryParameterName];

        var requestHeaders =
            GetValue<IDictionary<string, string[]>>(owinContext.Environment, "owin.RequestHeaders")
            ?? ImmutableDictionary<string, string[]>.Empty;

        var acceptOptions = new Dictionary<string, object>(StringComparer.Ordinal);
        if (requestHeaders.TryGetValue("Sec-WebSocket-Protocol", out string[]? subProtocols) && subProtocols.Length > 0) {
            // Select the first one from the client
            acceptOptions.Add("websocket.SubProtocol", subProtocols[0].Split(',').First().Trim());
        }

        acceptToken(acceptOptions, wsEnv => {
            var wsContext = (WebSocketContext)wsEnv["System.Net.WebSockets.WebSocketContext"];
            return HandleWebSocket(wsContext, clientId);
        });

        return HttpStatusCode.SwitchingProtocols;
    }

    private async Task HandleWebSocket(WebSocketContext wsContext, string clientId)
    {
        var serializers = Settings.SerializerFactory();
        var webSocket = wsContext.WebSocket;

        var wsChannel = new WebSocketChannel(webSocket);
        await using var _ = wsChannel.ConfigureAwait(false);

        var channel = wsChannel
            .WithTextSerializer(serializers)
            .WithId(clientId);
        Publisher.ChannelHub.Attach(channel);
        try {
            await wsChannel.WhenClosed.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }

    private static T? GetValue<T>(IDictionary<string, object?> env, string key)
        => env.TryGetValue(key, out var value) && value is T result ? result : default;
}
