using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;

namespace Stl.Fusion.Server;

public class WebSocketServer
{
    public class Options
    {
        public string RequestPath { get; set; } = "/fusion/ws";
        public string PublisherIdQueryParameterName { get; set; } = "publisherId";
        public string ClientIdQueryParameterName { get; set; } = "clientId";
        public Func<ITextSerializer<BridgeMessage>> SerializerFactory { get; set; } = DefaultSerializerFactory;

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
    protected Func<ITextSerializer<BridgeMessage>> SerializerFactory { get; }
    protected ILogger Log { get; }

    public string RequestPath { get; }
    public string PublisherIdQueryParameterName { get; }
    public string ClientIdQueryParameterName { get; }

    public WebSocketServer(Options? options, IPublisher publisher, ILogger<WebSocketServer>? log = null)
    {
        options ??= new();
        Log = log ?? NullLogger<WebSocketServer>.Instance;
        RequestPath = options.RequestPath;
        PublisherIdQueryParameterName = options.PublisherIdQueryParameterName;
        ClientIdQueryParameterName = options.ClientIdQueryParameterName;
        Publisher = publisher;
        SerializerFactory = options.SerializerFactory;
    }

    public async Task HandleRequest(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest) {
            context.Response.StatusCode = 400;
            return;
        }
        var publisherId = context.Request.Query[PublisherIdQueryParameterName];
        if (Publisher.Id != publisherId) {
            context.Response.StatusCode = 400;
            return;
        }

        var serializers = SerializerFactory();
        var clientId = context.Request.Query[ClientIdQueryParameterName];
        var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

        var wsChannel = new WebSocketChannel(webSocket);
        await using var _ = wsChannel.ConfigureAwait(false);

        var channel = wsChannel
            .WithTextSerializer(serializers)
            .WithId(clientId);
        Publisher.ChannelHub.Attach(channel);
        try {
            await wsChannel.WhenCompleted().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception e) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
