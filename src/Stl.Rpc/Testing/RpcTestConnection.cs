using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Testing;

public class RpcTestConnection
{
    private readonly object _lock = new();
    private volatile AsyncEvent<ChannelPair<RpcMessage>?> _connectionSource = new(null, true);
    private RpcClientPeer? _clientPeer;
    private RpcServerPeer? _serverPeer;

    public RpcTestClient TestClient { get; }
    public RpcHub Hub => TestClient.Hub;
    public RpcPeerRef ClientPeerRef { get; }
    public RpcPeerRef ServerPeerRef { get; }
    public RpcClientPeer ClientPeer => _clientPeer ??= Hub.GetClientPeer(ClientPeerRef);
    public RpcServerPeer ServerPeer => _serverPeer ??= Hub.GetServerPeer(ServerPeerRef);

    public RpcTestConnection(RpcTestClient testClient, RpcPeerRef clientPeerRef, RpcPeerRef serverPeerRef)
    {
        if (clientPeerRef.IsServer)
            throw new ArgumentOutOfRangeException(nameof(clientPeerRef));
        if (!serverPeerRef.IsServer)
            throw new ArgumentOutOfRangeException(nameof(serverPeerRef));

        TestClient = testClient;
        ClientPeerRef = clientPeerRef;
        ServerPeerRef = serverPeerRef;
    }

    public Task Connect(CancellationToken cancellationToken = default)
        => Connect(TestClient.Settings.ConnectionFactory.Invoke(TestClient), cancellationToken);

    public async Task Connect(ChannelPair<RpcMessage> connection, CancellationToken cancellationToken = default)
    {
        var clientConnectionState = ClientPeer.ConnectionState;
        var serverConnectionState = ServerPeer.ConnectionState;
        Disconnect();
        await clientConnectionState.WhenDisconnected(cancellationToken).ConfigureAwait(false);
        await serverConnectionState.WhenDisconnected(cancellationToken).ConfigureAwait(false);

        clientConnectionState = ClientPeer.ConnectionState;
        serverConnectionState = ServerPeer.ConnectionState;
        SetNextConnection(connection);
        await clientConnectionState.WhenConnected(cancellationToken).ConfigureAwait(false);
        await serverConnectionState.WhenConnected(cancellationToken).ConfigureAwait(false);
    }

    public void Disconnect(Exception? error = null)
    {
        ClientPeer.Disconnect(error);
        ServerPeer.Disconnect(error);
    }

    public Task Reconnect(CancellationToken cancellationToken = default)
        => Reconnect(null, cancellationToken);
    public async Task Reconnect(TimeSpan? connectDelay, CancellationToken cancellationToken = default)
    {
        Disconnect();
        var delay = (connectDelay ?? TimeSpan.FromMilliseconds(50)).Positive();
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        await Connect(cancellationToken).ConfigureAwait(false);
    }

    public void Terminate()
    {
        lock (_lock) {
            if (!_connectionSource.TryComplete(new ConnectionUnrecoverableException()))
                return;
        }
        ClientPeer.Disconnect();
        ServerPeer.Disconnect();
    }

    public async Task<Channel<RpcMessage>> PullClientChannel(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionSource = _connectionSource;
        while (true) {
            while (true) {
                if (connectionSource is { IsLatest: true, Value: not null })
                    break;

                connectionSource = await connectionSource.WhenNext(cancellationToken).ConfigureAwait(false);
                if (connectionSource == null)
                    throw Errors.ConnectionUnrecoverable();
            }

            lock (_lock) {
                if (_connectionSource == connectionSource) {
                    // It's truly the latest one, so we can pull the connection
                    _connectionSource = _connectionSource.TryAppendNext(null);
                    break;
                }
                connectionSource = _connectionSource;
            }
        }

        var connection = connectionSource.Value;
        var serverConnection = new RpcConnection(connection.Channel2);
        await ServerPeer.Connect(serverConnection, cancellationToken).ConfigureAwait(false);
        return connection.Channel1;
    }

    // Protected methods

    protected void SetNextConnection(ChannelPair<RpcMessage>? connection)
    {
        lock (_lock) {
            try {
                _connectionSource = _connectionSource.AppendNext(connection);
            }
            catch (InvalidOperationException) {
                throw Errors.ConnectionUnrecoverable();
            }
        }
    }
}
