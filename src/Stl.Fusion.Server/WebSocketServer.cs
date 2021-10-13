using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class WebSocketServer
    {
        public class Options
        {
            public string RequestPath { get; set; } = "/fusion/ws";
            public string PublisherIdQueryParameterName { get; set; } = "publisherId";
            public string ClientIdQueryParameterName { get; set; } = "clientId";
            public Func<IUtf16Serializer<BridgeMessage>> SerializerFactory { get; set; } = DefaultSerializerFactory;

            public static IUtf16Serializer<BridgeMessage> DefaultSerializerFactory()
                => new Utf16Serializer(
                    new TypeDecoratingSerializer(
                        SystemJsonSerializer.Default,
                        t => typeof(ReplicatorRequest).IsAssignableFrom(t)).Reader,
                    new TypeDecoratingSerializer(
                        SystemJsonSerializer.Default,
                        t => typeof(PublisherReply).IsAssignableFrom(t)).Writer
                    ).ToTyped<BridgeMessage>();
        }

        protected IPublisher Publisher { get; }
        protected Func<IUtf16Serializer<BridgeMessage>> SerializerFactory { get; }
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

            var serializers = SerializerFactory.Invoke();
            var clientId = context.Request.Query[ClientIdQueryParameterName];
            var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await using var wsChannel = new WebSocketChannel(webSocket);
            var channel = wsChannel
                .WithUtf16Serializer(serializers)
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
}
