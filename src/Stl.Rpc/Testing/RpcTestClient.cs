using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.WebSockets;

namespace Stl.Rpc.Testing;

public class RpcTestClient : RpcClient
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public BoundedChannelOptions ChannelOptions { get; init; } = WebSocketChannel<RpcMessage>.Options.Default.WriteChannelOptions;
        public Func<RpcTestClient, ChannelPair<RpcMessage>> ConnectionFactory { get; init; } = DefaultConnectionFactory;

        public static ChannelPair<RpcMessage> DefaultConnectionFactory(RpcTestClient testClient)
        {
            var settings = testClient.Settings;
            var channel1 = Channel.CreateBounded<RpcMessage>(settings.ChannelOptions);
            var channel2 = Channel.CreateBounded<RpcMessage>(settings.ChannelOptions);
            var connection = ChannelPair.CreateTwisted(channel1, channel2);
            return connection;
        }
    }

    private readonly ConcurrentDictionary<RpcPeerRef, RpcTestConnection> _peerStates = new();
    private long _lastPairId;

    public Options Settings { get; init; }
    public new RpcHub Hub => base.Hub;

    public RpcTestConnection this[RpcPeerRef peerRef]
        => _peerStates.GetValueOrDefault(peerRef) ?? throw new KeyNotFoundException();

    public RpcTestClient(Options settings, IServiceProvider services)
        : base(services)
        => Settings = settings;

    public RpcTestConnection Create()
    {
        var pairId = Interlocked.Increment(ref _lastPairId);
        return Create($"client-{pairId}", $"server-{pairId}");
    }

    public RpcTestConnection CreateDefault()
    {
        return Create(RpcPeerRef.Default.Id, "server-default");
    }

    public RpcTestConnection Create(Symbol clientId, Symbol serverId)
    {
        var clientPeerRef = new RpcPeerRef(typeof(RpcClientPeer), clientId);
        var serverPeerRef = new RpcPeerRef(typeof(RpcServerPeer), serverId);
        return Create(clientPeerRef, serverPeerRef);
    }

    public RpcTestConnection Create(RpcPeerRef clientPeerRef, RpcPeerRef serverPeerRef)
    {
        if (_peerStates.TryGetValue(clientPeerRef, out var peerState))
            return peerState;

        peerState = new RpcTestConnection(this, clientPeerRef, serverPeerRef);
        _peerStates.TryAdd(clientPeerRef, peerState);
        _peerStates.TryAdd(serverPeerRef, peerState);
        return peerState;
    }

    public override Task<Channel<RpcMessage>> CreateChannel(RpcClientPeer peer, CancellationToken cancellationToken)
        => Task.FromResult(this[peer].NextClientChannel());
}
