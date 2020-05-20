using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;
using Stl.Serialization;

namespace Stl.Samples.Blazor.Server.Services
{
    public class PublisherMiddleware
    {
        protected RequestDelegate Next { get; }
        protected IPublisher Publisher { get; }

        public PublisherMiddleware(
            RequestDelegate next,
            IPublisher publisher)
        {
            Next = next;
            Publisher = publisher;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != "/ws") {
                await Next.Invoke(context);
                return;
            }
            if (!context.WebSockets.IsWebSocketRequest) {
                context.Response.StatusCode = 400;
                return;
            }
            var clientId = context.Request.Query["clientId"];
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
