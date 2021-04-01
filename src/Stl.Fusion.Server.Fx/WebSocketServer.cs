using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Owin;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Net;
using Stl.Serialization;

using WebSocketAccept = System.Action<
    System.Collections.Generic.IDictionary<string, object>, // WebSocket Accept parameters
    System.Func< // WebSocketFunc callback
        System.Collections.Generic.IDictionary<string, object>, // WebSocket environment
        System.Threading.Tasks.Task>>;
using WebSocketCloseAsync = System.Func<
    int, // closeStatus
    string, // closeDescription
    System.Threading.CancellationToken, // cancel
    System.Threading.Tasks.Task>;
using WebSocketReceiveAsync = System.Func<
    System.ArraySegment<byte>, // data
    System.Threading.CancellationToken, // cancel
    System.Threading.Tasks.Task<
        System.Tuple< // WebSocketReceiveTuple
            int, // messageType
            bool, // endOfMessage
            int>>>; // count
// closeStatusDescription
using WebSocketReceiveResult = System.Tuple<int, bool, int>;
using WebSocketSendAsync = System.Func<
    System.ArraySegment<byte>, // data
    int, // message type
    bool, // end of message
    System.Threading.CancellationToken, // cancel
    System.Threading.Tasks.Task>;

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
        
        T GetValue<T>(IDictionary<string, object> env, string key) {
            object value;
            return env.TryGetValue(key, out value) && value is T ? (T)value : default(T);
        }
        
        
        public HttpStatusCode HandleRequest(IOwinContext owinContext)
        {
            // written based on https://stackoverflow.com/questions/41848095/websockets-using-owin
            
            var acceptToken = owinContext.Get<WebSocketAccept>("websocket.Accept");
            if (acceptToken == null)
                return HttpStatusCode.BadRequest;
            
            var publisherId = owinContext.Request.Query[PublisherIdQueryParameterName];
            if (Publisher.Id != publisherId)
                return HttpStatusCode.BadRequest;

            var clientId = owinContext.Request.Query[ClientIdQueryParameterName];

            var requestHeaders = GetValue<IDictionary<string, string[]>>(owinContext.Environment, "owin.RequestHeaders");

            Dictionary<string, object> acceptOptions = null;
            string[] subProtocols;
            if (requestHeaders.TryGetValue("Sec-WebSocket-Protocol", out subProtocols) && subProtocols.Length > 0) {
                acceptOptions = new Dictionary<string, object>();
                // Select the first one from the client
                acceptOptions.Add("websocket.SubProtocol", subProtocols[0].Split(',').First().Trim());
            }

            acceptToken(acceptOptions, c => ProcessSocketConnection(c, clientId));


            return HttpStatusCode.SwitchingProtocols;
        }

        private Task ProcessSocketConnection(IDictionary<string, object> wsEnv, string clientId)
        {
            var wsSendAsync = (WebSocketSendAsync)wsEnv["websocket.SendAsync"];
            var wsCloseAsync = (WebSocketCloseAsync)wsEnv["websocket.CloseAsync"];
            var wsCallCancelled = (CancellationToken)wsEnv["websocket.CallCancelled"];
            var wsRecieveAsync = (WebSocketReceiveAsync)wsEnv["websocket.ReceiveAsync"];
            var wsContext = (WebSocketContext)wsEnv["System.Net.WebSockets.WebSocketContext"];

            return HandleWebSocket(wsContext, clientId);
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
