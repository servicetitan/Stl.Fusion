using System;
using System.Net.Http;
using System.Net.WebSockets;
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

namespace Stl.Samples.Blazor.Common.Services
{
    public class Client : AsyncProcessBase
    {
        public Symbol Id { get; }
        protected ILogger Log { get; }
        protected IReplicator Replicator { get; }
        protected HttpClient HttpClient { get; }

        public Client(
            IReplicator replicator, 
            HttpClient httpClient, 
            ILogger<Client>? log = null)
        {
            Log = ((ILogger?) log) ?? NullLogger.Instance;
            Id = "c-" + new RandomStringGenerator().Next();
            Replicator = replicator;
            HttpClient = httpClient;
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var serverUri = HttpClient.BaseAddress;
            var wsUri = new Uri($"{ToWss(serverUri)}ws?clientId={Id.Value}");
            while (true) {
                try {
                    Log.LogInformation($"WebSocket: connecting to {wsUri}...");
                    var ws = new ClientWebSocket();
                    await ws.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                    Log.LogInformation("WebSocket: connected.");
                    await using var wsChannel = new WebSocketChannel(ws);
                    var channel = wsChannel
                        .WithLogger("channel", Log, LogLevel.Information)
                        .WithSerializer(new JsonNetSerializer<Message>())
                        .WithId(Symbol.Empty);
                    Replicator.ChannelHub.Attach(channel);
                    while (ws.State == WebSocketState.Open)
                        await Task.Delay(5000, default);
                    Log.LogInformation("WebSocket: closed.");
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    Log.LogError("WebSocket: error.", e);
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static Uri ToWss(Uri uri)
        {
            var url = uri.ToString();
            if (url.StartsWith("http://"))
                return new Uri("ws://" + url.Substring(7)); 
            if (url.StartsWith("https://"))
                return new Uri("wss://" + url.Substring(8)); 
            return uri;
        }
    }
}
