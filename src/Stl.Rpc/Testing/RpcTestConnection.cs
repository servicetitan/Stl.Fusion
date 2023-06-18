using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Testing;

public class RpcTestConnection
{
    private readonly object _lock = new();
    private ChannelPair<RpcMessage>? _nextChannelPair;
    private ChannelPair<RpcMessage>? _channelPair;

    public Channel<RpcMessage>? ClientChannel => _channelPair?.Channel1;
    public Channel<RpcMessage>? ServerChannel => _channelPair?.Channel2;

    public RpcTestClient TestClient { get; }
    public RpcHub Hub => TestClient.Hub;
    public RpcClientPeer ClientPeer { get; }
    public RpcServerPeer ServerPeer { get; }

    public RpcTestConnection(RpcTestClient testClient, RpcPeerRef clientPeerRef, RpcPeerRef serverPeerRef)
    {
        if (!clientPeerRef.IsClient)
            throw new ArgumentOutOfRangeException(nameof(clientPeerRef));
        if (!serverPeerRef.IsServer)
            throw new ArgumentOutOfRangeException(nameof(serverPeerRef));

        TestClient = testClient;
        ClientPeer = (RpcClientPeer)Hub.GetPeer(clientPeerRef);
        ServerPeer = (RpcServerPeer)Hub.GetPeer(serverPeerRef);
    }

    public void Connect()
        => Connect(TestClient.Settings.ConnectionFactory.Invoke(TestClient));

    public void Connect(ChannelPair<RpcMessage> connection)
    {
        lock (_lock) {
            DisconnectClient();
            _nextChannelPair = connection;
        }
    }

    public void DisconnectClient(Exception? error = null)
    {
        lock (_lock) {
            _channelPair?.Channel1.Writer.TryComplete(error);
        }
    }

    public void DisconnectServer(Exception? error = null)
    {
        lock (_lock) {
            _channelPair?.Channel2.Writer.TryComplete(error);
        }
    }

    public void Terminate()
    {
        lock (_lock) {
            DisconnectClient();
            _nextChannelPair = ChannelPair<RpcMessage>.Null;
        }
    }

    public Channel<RpcMessage> NextClientChannel()
    {
        lock (_lock) {
            var connection = _nextChannelPair;
            if (ReferenceEquals(connection, ChannelPair<RpcMessage>.Null))
                throw Errors.ConnectionUnrecoverable();

            _channelPair = connection ?? throw new InvalidOperationException("Connection is off now.");
            _nextChannelPair = null;
            ServerPeer.SetConnectionState(connection.Channel2);
            return connection.Channel1;
        }
    }
}
