using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class WebSocketServerMiddleware
    {
        public class Options
        {
            public string RequestPath { get; set; } = "/ws";
            public string PublisherIdQueryParameterName { get; set; } = "publisherId";
            public string ClientIdQueryParameterName { get; set; } = "clientId";
            public Func<ChannelSerializerPair<Message, string>> ChannelSerializerPairFactory { get; set; } = DefaultChannelSerializerPairFactory;

            public static ChannelSerializerPair<Message, string> DefaultChannelSerializerPairFactory()
                => new ChannelSerializerPair<Message, string>(
                    new JsonNetSerializer().ToTyped<Message>(),
                    new SafeJsonNetSerializer(t => typeof(ReplicatorMessage).IsAssignableFrom(t)).ToTyped<Message>());
        }

        protected string RequestPath { get; }
        protected string PublisherIdQueryParameterName { get; } 
        protected string ClientIdQueryParameterName { get; } 
        protected RequestDelegate Next { get; }
        protected IPublisher Publisher { get; }
        protected Func<ChannelSerializerPair<Message, string>> ChannelSerializerPairFactory { get; }

        public WebSocketServerMiddleware(
            RequestDelegate next,
            Options options,
            IPublisher publisher)
        {
            Next = next;
            RequestPath = options.RequestPath;
            PublisherIdQueryParameterName = options.PublisherIdQueryParameterName;
            ClientIdQueryParameterName = options.ClientIdQueryParameterName;
            Publisher = publisher;
            ChannelSerializerPairFactory = options.ChannelSerializerPairFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != RequestPath) {
                await Next.Invoke(context);
                return;
            }
            if (!context.WebSockets.IsWebSocketRequest) {
                context.Response.StatusCode = 400;
                return;
            }
            var publisherId = context.Request.Query[PublisherIdQueryParameterName];
            if (Publisher.Id != publisherId) {
                context.Response.StatusCode = 400;
                return;
            }

            var serializers = ChannelSerializerPairFactory.Invoke();
            var clientId = context.Request.Query[ClientIdQueryParameterName];
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await using var wsChannel = new WebSocketChannel(webSocket);
            var channel = wsChannel
                .WithSerializers(serializers)
                .WithId(clientId);
            Publisher.ChannelHub.Attach(channel);
            await wsChannel.ReaderTask.ConfigureAwait(false);
        }
    }
}
