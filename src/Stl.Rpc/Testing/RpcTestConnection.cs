using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Testing;

public class RpcTestConnection
{
    private readonly object _lock = new();
    private volatile AsyncEvent<ChannelPair<RpcMessage>?> _connectionSource = new(null, false);

    public RpcTestClient TestClient { get; }
    public RpcHub Hub => TestClient.Hub;
    public RpcClientPeer ClientPeer { get; }
    public RpcServerPeer ServerPeer { get; }
    // ReSharper disable once InconsistentlySynchronizedField
    public bool IsTerminated => _connectionSource.GetLatest().Value == ChannelPair<RpcMessage>.Null;

    public RpcTestConnection(RpcTestClient testClient, RpcPeerRef clientPeerRef, RpcPeerRef serverPeerRef)
    {
        if (clientPeerRef.IsServer)
            throw new ArgumentOutOfRangeException(nameof(clientPeerRef));
        if (!serverPeerRef.IsServer)
            throw new ArgumentOutOfRangeException(nameof(serverPeerRef));

        TestClient = testClient;
        ClientPeer = (RpcClientPeer)Hub.GetPeer(clientPeerRef);
        ServerPeer = (RpcServerPeer)Hub.GetPeer(serverPeerRef);
    }

    public Task Connect(CancellationToken cancellationToken = default)
        => Connect(TestClient.Settings.ConnectionFactory.Invoke(TestClient), cancellationToken);

    public async Task Connect(ChannelPair<RpcMessage> connection, CancellationToken cancellationToken = default)
    {
        Disconnect();
        await ClientPeer.ConnectionState
            .When(s => s.Channel == null, cancellationToken)
            .ConfigureAwait(false);
        await ServerPeer.ConnectionState
            .When(s => s.Channel == null, cancellationToken)
            .ConfigureAwait(false);

        SetNextConnection(connection);
        await ClientPeer.ConnectionState
            .When(s => s.Channel != null, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Disconnect(Exception? error = null)
    {
        ClientPeer.Disconnect(error);
        ServerPeer.Disconnect(error);
    }

    public void Terminate()
    {
        lock (_lock) {
            if (IsTerminated)
                return;

            _connectionSource = _connectionSource.CreateNext(ChannelPair<RpcMessage>.Null);
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
                if (connectionSource.Value == ChannelPair<RpcMessage>.Null)
                    throw Errors.ConnectionUnrecoverable();

                if (connectionSource is { IsLatest: true, Value: not null })
                    break;

                connectionSource = await connectionSource.WhenNext(cancellationToken).ConfigureAwait(false);
            }

            lock (_lock) {
                if (_connectionSource == connectionSource) {
                    _connectionSource = _connectionSource.CreateNext(null);
                    break;
                }
                connectionSource = _connectionSource;
            }
        }

        var connection = connectionSource.Value;
        await ServerPeer.Connect(connection.Channel2, cancellationToken).ConfigureAwait(false);
        return connection.Channel1;
    }

    public RpcTestConnection AssertNotTerminated()
        => !IsTerminated
            ? this
            : throw Errors.TestConnectionIsTerminated();

    // Protected methods

    protected void SetNextConnection(ChannelPair<RpcMessage>? connection)
    {
        lock (_lock) {
            AssertNotTerminated();
            _connectionSource = _connectionSource.CreateNext(connection);
        }
    }
}
