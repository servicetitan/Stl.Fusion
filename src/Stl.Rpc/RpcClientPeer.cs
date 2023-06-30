using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    private long _reconnectsAt;

    public RpcClientChannelFactory ChannelFactory { get; init; }
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
        ChannelFactory = Hub.ClientChannelFactory;
        ReconnectDelayer = Hub.ClientPeerReconnectDelayer;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken)
    {
        var (channel, error, _, tryIndex) = ConnectionState.LatestOrThrow().Value;
        if (channel != null)
            return channel;

        var (delayTask, endsAt) = ReconnectDelayer.Delay(this, tryIndex, error, cancellationToken);
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
        return await ChannelFactory.Invoke(this, cancellationToken).ConfigureAwait(false);
    }
}
