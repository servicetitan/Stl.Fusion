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
            public string ClientIdQueryParameterName { get; set; } = "clientId";
        }

        protected string RequestPath { get; }
        protected string ClientIdQueryParameterName { get; } 
        protected RequestDelegate Next { get; }
        protected IPublisher Publisher { get; }

        public WebSocketServerMiddleware(
            RequestDelegate next,
            Options options,
            IPublisher publisher)
        {
            Next = next;
            RequestPath = options.RequestPath;
            ClientIdQueryParameterName = options.ClientIdQueryParameterName;
            Publisher = publisher;
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
            var clientId = context.Request.Query[ClientIdQueryParameterName];
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await using var wsChannel = new WebSocketChannel(webSocket);
            var channel = wsChannel
                .WithSerializer(new JsonNetSerializer<Message>())
                .WithId(clientId);
            Publisher.ChannelHub.Attach(channel);
            await wsChannel.ReaderTask.ConfigureAwait(false);
        }
    }
}
