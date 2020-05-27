using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;
using Stl.Serialization;
using Stl.Text;

namespace Stl.Fusion.Client
{
    public class WebSocketChannelProvider : IChannelProvider
    {
        public class Options
        {
            public Uri BaseUri { get; set; } = new Uri("http://localhost:5000/");
            public string RequestPath { get; set; } = "/ws";
            public string PublisherIdQueryParameterName { get; set; } = "publisherId";
            public string ClientIdQueryParameterName { get; set; } = "clientId";
        }

        private readonly ILogger<WebSocketChannelProvider> _log;

        public Uri BaseUri { get; }
        public string RequestPath { get; }
        public string PublisherIdQueryParameterName { get; }
        public string ClientIdQueryParameterName { get; }
        protected Lazy<IReplicator>? ReplicatorLazy { get; }
        protected Symbol ClientId => ReplicatorLazy?.Value.Id ?? Symbol.Empty; 

        public WebSocketChannelProvider(
            Options options,
            Lazy<IReplicator>? replicatorLazy = null, 
            ILogger<WebSocketChannelProvider>? log = null)
        {
            _log = log ??= NullLogger<WebSocketChannelProvider>.Instance;

            BaseUri = options.BaseUri;
            RequestPath = options.RequestPath;
            PublisherIdQueryParameterName = options.PublisherIdQueryParameterName;
            ClientIdQueryParameterName = options.ClientIdQueryParameterName;
            ReplicatorLazy = replicatorLazy;
        }

        public async Task<Channel<Message>> CreateChannelAsync(
            Symbol publisherId, CancellationToken cancellationToken)
        {
            var clientId = ClientId.Value;
            try {
                var connectionUri = GetConnectionUrl(publisherId);
                _log.LogInformation($"{clientId}: connecting to {connectionUri}...");
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(connectionUri, cancellationToken).ConfigureAwait(false);
                _log.LogInformation($"{clientId}: connected.");
                
                await using var wsChannel = new WebSocketChannel(ws);
                var channel = wsChannel
                    .WithLogger(clientId, _log, LogLevel.Information)
                    .WithSerializer(new SafeJsonNetSerializer<Message>());
                return channel;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                _log.LogError($"{clientId}: error: {e.GetType().Name}(\"{e.Message}\").", e);
                throw;
            }
        }

        protected virtual Uri GetConnectionUrl(Symbol publisherId)
        {
            var url = BaseUri.ToString();
            if (url.StartsWith("http://"))
                url = "ws://" + url.Substring(7); 
            else if (url.StartsWith("https://"))
                url = "wss://" + url.Substring(8);
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            url += RequestPath;
            var uriBuilder = new UriBuilder(url);
            var queryTail = 
                $"{PublisherIdQueryParameterName}={publisherId.Value}" +
                $"&{ClientIdQueryParameterName}={ClientId.Value}";
            if (!string.IsNullOrEmpty(uriBuilder.Query))
                uriBuilder.Query += "&" + queryTail;
            else
                uriBuilder.Query = queryTail;
            return uriBuilder.Uri;
        }
    }
}
