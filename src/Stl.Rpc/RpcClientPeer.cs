namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    private long _reconnectsAt;

    public RpcClientConnectionFactory ConnectionFactory { get; init; }
    public RpcClientPeerReconnectDelayer ReconnectDelayer { get; init; }

    public Moment? ReconnectsAt {
        get {
            var reconnectsAt = Interlocked.Read(ref _reconnectsAt);
            return reconnectsAt == default ? null : new Moment(reconnectsAt);
        }
    }

    public RpcClientPeer(RpcHub hub, RpcPeerRef @ref)
        : base(hub, @ref)
    {
        LocalServiceFilter = static _ => false;
        ConnectionFactory = Hub.ClientConnectionFactory;
        ReconnectDelayer = Hub.ClientPeerReconnectDelayer;
    }

    // Protected methods

    protected override async Task<RpcConnection> GetConnection(CancellationToken cancellationToken)
    {
        var (connection, error, _, tryIndex) = ConnectionState.LatestOrThrowIfCompleted().Value;
        if (connection != null)
            return connection;

        var (delayTask, endsAt) = ReconnectDelayer.GetDelay(this, tryIndex, error, cancellationToken);
        if (!delayTask.IsCompleted) {
            Interlocked.Exchange(ref _reconnectsAt, endsAt.EpochOffsetTicks);
            try {
                await delayTask.ConfigureAwait(false);
            }
            finally {
                Interlocked.Exchange(ref _reconnectsAt, 0);
            }
        }

        Log.LogInformation("'{PeerRef}': Connecting...", Ref);
        return await ConnectionFactory.Invoke(this, cancellationToken).ConfigureAwait(false);
    }
}
