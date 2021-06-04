using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Channels;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;
using Stl.Net;
using Stl.Serialization;
using Stl.Text;

namespace Stl.Fusion.Client
{
    public class WebSocketChannelProvider : IChannelProvider, IHasServices
    {
        public class Options
        {
            public Uri BaseUri { get; set; } = new("http://localhost:5000/");
            public string RequestPath { get; set; } = "/fusion/ws";
            public string PublisherIdQueryParameterName { get; set; } = "publisherId";
            public string ClientIdQueryParameterName { get; set; } = "clientId";
            public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
            public int? MessageMaxLength { get; set; } = 2048;
            public Func<IServiceProvider, TypedSerializer<BridgeMessage, string>> TypedSerializerFactory { get; set; } =
                DefaultTypedSerializerFactory;
            public Func<IServiceProvider, ClientWebSocket> ClientWebSocketFactory { get; set; } =
                DefaultClientWebSocketFactory;
            public Func<WebSocketChannelProvider, Symbol, Uri> ConnectionUrlResolver { get; set; } =
                DefaultConnectionUrlResolver;
            public bool IsLoggingEnabled { get; set; } = true;
            public bool IsMessageLoggingEnabled { get; set; } = false;

            public static TypedSerializer<BridgeMessage, string> DefaultTypedSerializerFactory(IServiceProvider services)
                => new(
                    new SafeJsonNetSerializer(t => typeof(ReplicatorMessage).IsAssignableFrom(t)).ToTyped<BridgeMessage>().Serializer,
                    new JsonNetSerializer().ToTyped<BridgeMessage>().Deserializer);

            public static ClientWebSocket DefaultClientWebSocketFactory(IServiceProvider services)
                => services?.GetService<ClientWebSocket>() ?? new ClientWebSocket();

            public static Uri DefaultConnectionUrlResolver(WebSocketChannelProvider channelProvider, Symbol publisherId)
            {
                var url = channelProvider.BaseUri.ToString();
                if (url.StartsWith("http://"))
                    url = "ws://" + url.Substring(7);
                else if (url.StartsWith("https://"))
                    url = "wss://" + url.Substring(8);
                if (url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);
                url += channelProvider.RequestPath;
                var uriBuilder = new UriBuilder(url);
                var queryTail =
                    $"{channelProvider.PublisherIdQueryParameterName}={publisherId.Value}" +
                    $"&{channelProvider.ClientIdQueryParameterName}={channelProvider.ClientId.Value}";
                if (!string.IsNullOrEmpty(uriBuilder.Query))
                    uriBuilder.Query += "&" + queryTail;
                else
                    uriBuilder.Query = queryTail;
                return uriBuilder.Uri;
            }
        }

        protected Func<IServiceProvider, TypedSerializer<BridgeMessage, string>> TypedSerializerFactory { get; }
        protected Func<IServiceProvider, ClientWebSocket> ClientWebSocketFactory { get; }
        protected int? MessageMaxLength { get; }
        protected Lazy<IReplicator>? ReplicatorLazy { get; }
        protected Symbol ClientId => ReplicatorLazy?.Value.Id ?? Symbol.Empty;
        protected ILogger Log { get; }
        protected bool IsLoggingEnabled { get; set; }
        protected bool IsMessageLoggingEnabled { get; set; }
        protected LogLevel LogLevel { get; set; } = LogLevel.Information;
        protected LogLevel MessageLogLevel { get; set; } = LogLevel.Information;

        public Uri BaseUri { get; }
        public Func<WebSocketChannelProvider, Symbol, Uri> ConnectionUrlResolver { get; }
        public string RequestPath { get; }
        public string PublisherIdQueryParameterName { get; }
        public string ClientIdQueryParameterName { get; }
        public TimeSpan ConnectTimeout { get; }
        public IServiceProvider Services { get; }

        public WebSocketChannelProvider(
            Options? options,
            IServiceProvider services,
            ILogger<WebSocketChannelProvider>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<WebSocketChannelProvider>.Instance;
            IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);
            IsMessageLoggingEnabled = options.IsMessageLoggingEnabled && Log.IsEnabled(MessageLogLevel);

            Services = services;
            BaseUri = options.BaseUri;
            RequestPath = options.RequestPath;
            PublisherIdQueryParameterName = options.PublisherIdQueryParameterName;
            ClientIdQueryParameterName = options.ClientIdQueryParameterName;
            MessageMaxLength = options.MessageMaxLength;
            ConnectTimeout = options.ConnectTimeout;
            ReplicatorLazy = new Lazy<IReplicator>(services.GetRequiredService<IReplicator>);
            TypedSerializerFactory = options.TypedSerializerFactory;
            ClientWebSocketFactory = options.ClientWebSocketFactory;
            ConnectionUrlResolver = options.ConnectionUrlResolver;
        }

        public async Task<Channel<BridgeMessage>> CreateChannel(
            Symbol publisherId, CancellationToken cancellationToken)
        {
            var clientId = ClientId.Value;
            try {
                var connectionUri = ConnectionUrlResolver.Invoke(this, publisherId);
                if (IsLoggingEnabled)
                    Log.Log(LogLevel, "{ClientId}: connecting to {ConnectionUri}...", clientId, connectionUri);
                var ws = ClientWebSocketFactory.Invoke(Services);
                using var cts = new CancellationTokenSource(ConnectTimeout);
                using var lts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                await ws.ConnectAsync(connectionUri, lts.Token).ConfigureAwait(false);
                if (IsLoggingEnabled)
                    Log.Log(LogLevel, "{ClientId}: connected", clientId);

                var wsChannel = new WebSocketChannel(ws);
                Channel<string> stringChannel = wsChannel;
                if (IsMessageLoggingEnabled)
                    stringChannel = stringChannel.WithLogger(clientId, Log, MessageLogLevel, MessageMaxLength);
                var serializers = TypedSerializerFactory.Invoke(Services);
                var resultChannel = stringChannel.WithSerializers(serializers);
                wsChannel.WhenCompleted(CancellationToken.None).ContinueWith(async _ => {
                    await Task.Delay(1000, default).ConfigureAwait(false);
                    await wsChannel.DisposeAsync().ConfigureAwait(false);
                }, CancellationToken.None).Ignore();
                return resultChannel;
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw Errors.WebSocketConnectTimeout();
            }
            catch (Exception e) {
                Log.LogError(e, "{ClientId}: error", clientId);
                throw;
            }
        }
    }
}
