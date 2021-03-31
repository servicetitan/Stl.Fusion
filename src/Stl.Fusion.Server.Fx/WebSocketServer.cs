using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Net;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class WebSocketServer
    {
        public class Options
        {
            private static readonly WebSocketChannelProvider.Options DefaultClientOptions = new();

            public string RequestPath { get; set; } = DefaultClientOptions.RequestPath;
            public string PublisherIdQueryParameterName { get; set; } = DefaultClientOptions.PublisherIdQueryParameterName;
            public string ClientIdQueryParameterName { get; set; } = DefaultClientOptions.ClientIdQueryParameterName;
            public Func<ChannelSerializerPair<BridgeMessage, string>> ChannelSerializerPairFactory { get; set; } =
                DefaultChannelSerializerPairFactory;

            public static ChannelSerializerPair<BridgeMessage, string> DefaultChannelSerializerPairFactory()
                => new(
                    new JsonNetSerializer().ToTyped<BridgeMessage>(),
                    new SafeJsonNetSerializer(t => typeof(ReplicatorMessage).IsAssignableFrom(t)).ToTyped<BridgeMessage>());
        }

        protected IPublisher Publisher { get; }
        protected Func<ChannelSerializerPair<BridgeMessage, string>> ChannelSerializerPairFactory { get; }
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
            ChannelSerializerPairFactory = options.ChannelSerializerPairFactory;
        }

        public async Task HandleRequest(HttpContext context)
        {
            if (!context.IsWebSocketRequest) {
                context.Response.StatusCode = 400;
                return;
            }
            var publisherId = context.Request.QueryString[PublisherIdQueryParameterName];
            if (Publisher.Id != publisherId) {
                context.Response.StatusCode = 400;
                return;
            }

            var clientId = context.Request.QueryString[ClientIdQueryParameterName];
            context.AcceptWebSocketRequest(ctx => HandleWebSocket(ctx, clientId));
        }
        
        private async Task HandleWebSocket(WebSocketContext wsContext, string clientId)
        {
            var serializers = ChannelSerializerPairFactory.Invoke();
            var webSocket = wsContext.WebSocket;
            await using var wsChannel = new WebSocketChannel(webSocket);
            var channel = wsChannel
                .WithSerializers(serializers)
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
