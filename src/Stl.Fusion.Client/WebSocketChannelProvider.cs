using System.Net.WebSockets;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;
using Stl.Net;

namespace Stl.Fusion.Client;

public class WebSocketChannelProvider : IChannelProvider, IHasServices
{
    public record Options
    {
        public Uri BaseUri { get; init; } = new("http://localhost:5000/");
        public string RequestPath { get; init; } = "/fusion/ws";
        public string PublisherIdQueryParameterName { get; init; } = "publisherId";
        public string ClientIdQueryParameterName { get; init; } = "clientId";
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);
        public int? MessageMaxLength { get; init; } = 2048;
        public Func<IServiceProvider, ITextSerializer<BridgeMessage>> SerializerFactory { get; init; } =
            DefaultSerializerFactory;
        public Func<IServiceProvider, ClientWebSocket> ClientWebSocketFactory { get; init; } =
            DefaultClientWebSocketFactory;
        public Func<WebSocketChannelProvider, Symbol, Uri> ConnectionUrlResolver { get; init; } =
            DefaultConnectionUrlResolver;
        public IMomentClock? Clock { get; init; } = null;
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
        public LogLevel MessageLogLevel { get; init; } = LogLevel.None;

        public static ITextSerializer<BridgeMessage> DefaultSerializerFactory(IServiceProvider services)
            => TextSerializer.NewAsymmetric(
                new TypeDecoratingSerializer(
                    SystemJsonSerializer.Default,
                    t => typeof(PublisherReply).IsAssignableFrom(t)),
                new TypeDecoratingSerializer(
                    SystemJsonSerializer.Default,
                    t => typeof(ReplicatorRequest).IsAssignableFrom(t)
            )).ToTyped<BridgeMessage>();

        public static ClientWebSocket DefaultClientWebSocketFactory(IServiceProvider services)
            => services.GetService<ClientWebSocket>() ?? new ClientWebSocket();

        public static Uri DefaultConnectionUrlResolver(
            WebSocketChannelProvider channelProvider,
            Symbol publisherId)
        {
            var settings = channelProvider.Settings;
            var url = settings.BaseUri.ToString();
            if (url.StartsWith("http://", StringComparison.Ordinal))
                url = "ws://" + url.Substring(7);
            else if (url.StartsWith("https://", StringComparison.Ordinal))
                url = "wss://" + url.Substring(8);
            if (url.EndsWith("/", StringComparison.Ordinal))
                url = url.Substring(0, url.Length - 1);
            url += settings.RequestPath;
            var uriBuilder = new UriBuilder(url);
            var queryTail =
                $"{settings.PublisherIdQueryParameterName}={publisherId.Value}" +
                $"&{settings.ClientIdQueryParameterName}={channelProvider.ClientId.Value}";
            if (!string.IsNullOrEmpty(uriBuilder.Query))
                uriBuilder.Query += "&" + queryTail;
            else
                uriBuilder.Query = queryTail;
            return uriBuilder.Uri;
        }
    }

    private IReplicator? _replicator;

    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected bool IsMessageLoggingEnabled { get; set; }

    protected IReplicator Replicator => _replicator ??= Services.GetRequiredService<IReplicator>();
    protected Symbol ClientId => Replicator.Id;
    protected IMomentClock Clock { get; }

    public Options Settings { get; }
    public IServiceProvider Services { get; }

    public WebSocketChannelProvider(Options settings,
        IServiceProvider services,
        ILogger<WebSocketChannelProvider>? log = null)
    {
        Settings = settings;
        Services = services;
        Log = Services.LogFor(GetType());
        IsLoggingEnabled = Log.IsLogging(settings.LogLevel);
        IsMessageLoggingEnabled = Log.IsLogging(settings.MessageLogLevel);

        Clock = settings.Clock ?? Services.Clocks().CpuClock;
    }

    public async Task<Channel<BridgeMessage>> CreateChannel(
        Symbol publisherId, CancellationToken cancellationToken)
    {
        var clientId = ClientId.Value;
        var cts = cancellationToken.CreateLinkedTokenSource();
        try {
            var connectionUri = Settings.ConnectionUrlResolver(this, publisherId);
            if (IsLoggingEnabled)
                Log.Log(Settings.LogLevel,
                    "{ClientId}: connecting to {ConnectionUri}...", clientId, connectionUri);
            var ws = Settings.ClientWebSocketFactory(Services);

            try {
                await ws.ConnectAsync(connectionUri, cts.Token)
                    .WaitAsync(Clock, Settings.ConnectTimeout, cts.Token)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException) {
                throw Errors.WebSocketConnectTimeout();
            }
            if (IsLoggingEnabled)
                Log.Log(Settings.LogLevel, "{ClientId}: connected", clientId);

            var wsChannel = new WebSocketChannel(ws);
            Channel<string> stringChannel = wsChannel;
            if (IsMessageLoggingEnabled)
                stringChannel = stringChannel.WithLogger(
                    clientId, Log, Settings.MessageLogLevel, Settings.MessageMaxLength);
            var serializers = Settings.SerializerFactory(Services);
            var resultChannel = stringChannel.WithTextSerializer(serializers);
            _ = wsChannel.WhenClosed().ContinueWith(_ => wsChannel.DisposeAsync(),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return resultChannel;
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "{ClientId}: error", clientId);
            throw;
        }
        finally {
            cts.CancelAndDisposeSilently();
        }
    }
}
