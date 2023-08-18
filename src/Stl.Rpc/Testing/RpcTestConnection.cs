using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Testing;

public class RpcTestConnection
{
    private readonly object _lock = new();
    private volatile AsyncState<ChannelPair<RpcMessage>?> _channels = new(null, true);
    private RpcClientPeer? _clientPeer;
    private RpcServerPeer? _serverPeer;

    public RpcTestClient TestClient { get; }
    public RpcHub Hub => TestClient.Hub;
    public RpcPeerRef ClientPeerRef { get; }
    public RpcPeerRef ServerPeerRef { get; }
    public RpcClientPeer ClientPeer => _clientPeer ??= Hub.GetClientPeer(ClientPeerRef);
    public RpcServerPeer ServerPeer => _serverPeer ??= Hub.GetServerPeer(ServerPeerRef);

    public ChannelPair<RpcMessage>? Channels {
        // ReSharper disable once InconsistentlySynchronizedField
        get => _channels.Last.Value;
        protected set {
            lock (_lock) {
                if (_channels.IsFinal)
                    return;
                if (ReferenceEquals(_channels.Value, value))
                    return;

                _channels = _channels.SetNext(value);
            }
        }
    }

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

    public async Task Connect(ChannelPair<RpcMessage> channels, CancellationToken cancellationToken = default)
    {
        var clientConnectionState = ClientPeer.ConnectionState;
        var serverConnectionState = ServerPeer.ConnectionState;
        Disconnect();
        await clientConnectionState.WhenDisconnected(cancellationToken).ConfigureAwait(false);
        await serverConnectionState.WhenDisconnected(cancellationToken).ConfigureAwait(false);

        clientConnectionState = ClientPeer.ConnectionState;
        serverConnectionState = ServerPeer.ConnectionState;
        Channels = channels;
        await clientConnectionState.WhenConnected(cancellationToken).ConfigureAwait(false);
        await serverConnectionState.WhenConnected(cancellationToken).ConfigureAwait(false);
    }

    public void Disconnect(Exception? error = null)
    {
        Channels = null;
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
            if (_channels.IsFinal)
                return;

            if (!ReferenceEquals(_channels.Value, null))
                _channels = _channels.SetNext(null);
            _channels.SetFinal(new ConnectionUnrecoverableException());
        }
        ClientPeer.Disconnect();
        ServerPeer.Disconnect();
    }

    public async Task<Channel<RpcMessage>> PullClientChannel(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var channels = await WhenConnected(cancellationToken).ConfigureAwait(false);
        var serverConnection = new RpcConnection(channels.Channel2);
        await ServerPeer.Connect(serverConnection, cancellationToken).ConfigureAwait(false);
        return channels.Channel1;
    }

    // Protected methods

    protected async ValueTask<ChannelPair<RpcMessage>> WhenConnected(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        await foreach (var channels in _channels.Last.Changes(cancellationToken).ConfigureAwait(false)) {
            if (channels == null)
                continue;
            if (channels.Channel1.Reader.Completion.IsCompleted)
                continue;
            if (channels.Channel2.Reader.Completion.IsCompleted)
                continue;

            return channels;
        }
        // Impossible to get here, but we still need to return something, so...
        throw Errors.ConnectionUnrecoverable();
    }
}
