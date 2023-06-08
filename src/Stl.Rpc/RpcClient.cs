using System.Net.WebSockets;
using System.Text.Encodings.Web;
using Stl.Generators;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc;

public class RpcClient : IHasServices
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public Func<RpcClientPeer, string> HostUrlResolver { get; init; } = peer => peer.Name.Value;
        public string RequestPath { get; init; } = "/rpc/ws";
        public string ClientIdParameterName { get; init; } = "clientId";
        public Func<IServiceProvider, string> ClientIdProvider { get; init; } = DefaultClientIdProvider;
        public Func<IServiceProvider, ClientWebSocket> WebSocketFactory { get; init; } = DefaultClientWebSocketFactory;
        public WebSocketChannel2<RpcMessage>.Options WebSocketChannelOptions { get; init; } = WebSocketChannel2<RpcMessage>.Options.Default;
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

        public static string DefaultClientIdProvider(IServiceProvider services)
            => "c-" + RandomStringGenerator.Default.Next(32);

        public static ClientWebSocket DefaultClientWebSocketFactory(IServiceProvider services)
            => services.GetService<ClientWebSocket>() ?? new ClientWebSocket();
    }

    private ILogger? _log;
    private RpcHub? _rpcHub;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public RpcHub RpcHub => _rpcHub ??= Services.GetRequiredService<RpcHub>();
    public Symbol ClientId { get; }

    public RpcClient(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        ClientId = settings.ClientIdProvider.Invoke(services);
    }

    public async Task<Channel<RpcMessage>> GetChannel(RpcClientPeer peer, CancellationToken cancellationToken)
    {
        var uri = GetConnectionUri(peer);
        var webSocket = Settings.WebSocketFactory(Services);
        await webSocket.ConnectAsync(uri, cancellationToken)
            .WaitAsync(Settings.ConnectTimeout, cancellationToken)
            .ConfigureAwait(false);
        var channel = new WebSocketChannel2<RpcMessage>(Settings.WebSocketChannelOptions, webSocket);
        return channel;
    }

    // Protected methods

    protected virtual Uri GetConnectionUri(RpcClientPeer peer)
    {
        var settings = Settings;
        var url = settings.HostUrlResolver.Invoke(peer).TrimSuffix("/");
        if (url.StartsWith("http://", StringComparison.Ordinal))
            url = "ws://" + url.Substring(7);
        else if (url.StartsWith("https://", StringComparison.Ordinal))
            url = "wss://" + url.Substring(8);
        else
            url = "wss://" + url;
        url += settings.RequestPath;

        var uriBuilder = new UriBuilder(url);
        var queryTail = $"{settings.ClientIdParameterName}={UrlEncoder.Default.Encode(ClientId)}";
        if (!uriBuilder.Query.IsNullOrEmpty())
            uriBuilder.Query += "&" + queryTail;
        else
            uriBuilder.Query = queryTail;
        return uriBuilder.Uri;
    }
}
