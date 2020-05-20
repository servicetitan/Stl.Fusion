using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Net;
using Stl.Security;
using Stl.Serialization;
using Stl.Text;

namespace Stl.Fusion.Client
{
    public class WebSocketClient : AsyncProcessBase
    {
        public class Options
        {
            protected static Symbol NewId() => "c-" + RandomStringGenerator.Default.Next();

            private Uri? _connectionUri;

            public Symbol Id { get; set; } = NewId();
            public Uri BaseUri { get; set; } = new Uri("http://localhost:5000/");
            public string RequestPath { get; set; } = "/ws";
            public string ClientIdQueryParameterName { get; set; } = "clientId";
            public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

            public Uri ConnectionUri {
                get => _connectionUri ?? GetConnectionUrl();
                set => _connectionUri = value;
            }

            protected virtual Uri GetConnectionUrl()
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
                var queryTail = $"{ClientIdQueryParameterName}={Id.Value}";
                if (!string.IsNullOrEmpty(uriBuilder.Query))
                    uriBuilder.Query += "&" + queryTail;
                else
                    uriBuilder.Query = queryTail;
                return uriBuilder.Uri;
            }
        }

        private readonly ILogger<WebSocketClient> _log;

        public Symbol Id { get; }
        public Uri ConnectionUri { get; } 
        public TimeSpan ReconnectDelay { get; }
        protected IReplicator Replicator { get; }

        public WebSocketClient(
            Options options,
            IReplicator replicator,
            TimeSpan? reconnectDelay = null,
            ILogger<WebSocketClient>? log = null)
        {
            reconnectDelay ??= TimeSpan.FromSeconds(5);
            log ??= NullLogger<WebSocketClient>.Instance;

            _log = log;
            Id = options.Id;
            ConnectionUri = options.ConnectionUri;
            ReconnectDelay = options.ReconnectDelay;
            Replicator = replicator;
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            while (true) {
                try {
                    _log.LogInformation($"{Id}: connecting to {ConnectionUri}...");
                    var ws = new ClientWebSocket();
                    await ws.ConnectAsync(ConnectionUri, CancellationToken.None).ConfigureAwait(false);
                    _log.LogInformation($"{Id}: connected.");
                    
                    await using var wsChannel = new WebSocketChannel(ws);
                    var channel = wsChannel
                        .WithLogger(Id.ToString(), _log, LogLevel.Information)
                        .WithSerializer(new JsonNetSerializer<Message>())
                        .WithId(Symbol.Empty);
                    Replicator.ChannelHub.Attach(channel);
                    
                    await channel.Reader.Completion
                        .WithFakeCancellation(cancellationToken)
                        .ConfigureAwait(false);
                    _log.LogInformation($"{Id}: disconnected.");
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    _log.LogError($"{Id}: error.", e);
                    await Task.Delay(ReconnectDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
