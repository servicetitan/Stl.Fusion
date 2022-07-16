using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;

namespace Stl.Fusion.Server;

public class WebSocketServer
{
    public record Options
    {
        public string RequestPath { get; init; } = "/fusion/ws";
        public string PublisherIdQueryParameterName { get; init; } = "publisherId";
        public string ClientIdQueryParameterName { get; init; } = "clientId";
        public Func<ITextSerializer<BridgeMessage>> SerializerFactory { get; init; } = DefaultSerializerFactory;

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
        if (!context.WebSockets.IsWebSocketRequest) {
            context.Response.StatusCode = 400;
            return;
        }
        var publisherId = context.Request.Query[Settings.PublisherIdQueryParameterName];
        if (Publisher.Id != publisherId) {
            context.Response.StatusCode = 400;
            return;
        }

        var serializers = Settings.SerializerFactory();
        var clientId = context.Request.Query[Settings.ClientIdQueryParameterName];
        var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

        var wsChannel = new WebSocketChannel(webSocket);
        await using var _ = wsChannel.ConfigureAwait(false);

        var channel = wsChannel
            .WithTextSerializer(serializers)
            .WithId(clientId);
        Publisher.ChannelHub.Attach(channel);
        try {
            await wsChannel.WhenClosed().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogWarning(e, "WebSocket connection was closed with an error");
        }
    }
}
