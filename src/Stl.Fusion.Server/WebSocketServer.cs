using System.Net;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Rpc;

namespace Stl.Fusion.Server;

public class WebSocketServer
{
    public record Options
    {
        public string RequestPath { get; init; } = "/fusion/ws";
        public string PublisherIdQueryParameterName { get; init; } = "publisherId";
        public string ClientIdQueryParameterName { get; init; } = "clientId";
        public Func<ITextSerializer<BridgeMessage>> SerializerFactory { get; init; } = DefaultSerializerFactory;
#if NET6_0_OR_GREATER
        public Func<WebSocketAcceptContext> ConfigureWebSocket { get; init; } = () => new();
#endif

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

    public async Task HandleRequest(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        if (!context.WebSockets.IsWebSocketRequest) {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return;
        }

        var requestQuery = context.Request.Query;
        var clientId = requestQuery[Settings.ClientIdQueryParameterName];
        var publisherId = requestQuery[Settings.PublisherIdQueryParameterName];

        var serializers = Settings.SerializerFactory();
#if NET6_0_OR_GREATER
        var webSocketAcceptContext = Settings.ConfigureWebSocket.Invoke();
        var acceptWebSocketTask = context.WebSockets.AcceptWebSocketAsync(webSocketAcceptContext);
#else
        var acceptWebSocketTask = context.WebSockets.AcceptWebSocketAsync();
#endif
        var webSocket = await acceptWebSocketTask.ConfigureAwait(false);
        var wsChannel = new WebSocketChannel(webSocket);
        await using var _ = wsChannel.ConfigureAwait(false);

        var channel = wsChannel
            .WithTextSerializer(serializers)
            .WithId(clientId);

        var welcomeReply = new WelcomeReply() {
            PublisherId = Publisher.Id,
            MessageIndex = -1,
            IsAccepted = Publisher.Id == publisherId,
        };
        await channel.Writer.WriteAsync(welcomeReply, cancellationToken).ConfigureAwait(false);

        if (welcomeReply.IsAccepted)
            Publisher.ChannelHub.Attach(channel);
        else
            channel.Writer.TryComplete();

        try {
            await wsChannel.WhenClosed().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
