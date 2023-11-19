using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc.Clients;

public class RpcWebSocketClient(
        RpcWebSocketClient.Options settings,
        IServiceProvider services
        ) : RpcClient(services)
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public Func<RpcWebSocketClient, RpcClientPeer, string> HostUrlResolver { get; init; } = DefaultHostUrlResolver;
        public Func<RpcWebSocketClient, RpcClientPeer, Uri> ConnectionUriResolver { get; init; } = DefaultConnectionUriResolver;
        public WebSocketChannel<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel<RpcMessage>.Options.Default;
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);
        public string RequestPath { get; init; } = "/rpc/ws";
        public string ClientIdParameterName { get; init; } = "clientId";

        public static string DefaultHostUrlResolver(RpcWebSocketClient client, RpcClientPeer peer)
            => peer.Ref.Key.Value;

        public static Uri DefaultConnectionUriResolver(RpcWebSocketClient client, RpcClientPeer peer)
        {
            var settings = client.Settings;
            var url = settings.HostUrlResolver.Invoke(client, peer).TrimSuffix("/");
            var isWebSocketUrl = url.StartsWith("ws://", StringComparison.Ordinal)
                || url.StartsWith("wss://", StringComparison.Ordinal);
            if (!isWebSocketUrl) {
                if (url.StartsWith("http://", StringComparison.Ordinal))
                    url = "ws://" + url[7..];
                else if (url.StartsWith("https://", StringComparison.Ordinal))
                    url = "wss://" + url[8..];
                else
                    url = "wss://" + url;
                url += settings.RequestPath;
            }

            var uriBuilder = new UriBuilder(url);
            var queryTail = $"{settings.ClientIdParameterName}={UrlEncoder.Default.Encode(client.ClientId)}";
            if (!uriBuilder.Query.IsNullOrEmpty())
                uriBuilder.Query += "&" + queryTail;
            else
                uriBuilder.Query = queryTail;
            return uriBuilder.Uri;
        }
    }

    public Options Settings { get; } = settings;

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public override async Task<RpcConnection> CreateConnection(RpcClientPeer peer, CancellationToken cancellationToken)
    {
        var uri = Settings.ConnectionUriResolver(this, peer);
        using var cts = new CancellationTokenSource(Settings.ConnectTimeout);
        var ctsToken = cts.Token;
        // ReSharper disable once UseAwaitUsing
        using var _ = cancellationToken.Register(static x => (x as CancellationTokenSource)?.Cancel(), cts);
        var webSocket = await Task.Run(async () => {
                var ws = Services.GetRequiredService<ClientWebSocket>();
                await ws.ConnectAsync(uri, ctsToken).ConfigureAwait(false);
                return ws;
            }, ctsToken)
            .WaitAsync(ctsToken) // MAUI sometimes stuck in sync part of ConnectAsync
            .ConfigureAwait(false);

        var channel = new WebSocketChannel<RpcMessage>(Settings.WebSocketChannelOptions, webSocket, Services);
        var options = ImmutableOptionSet.Empty.Set(uri).Set(webSocket);
        return new RpcConnection(channel, options);
    }
}
