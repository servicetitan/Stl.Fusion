using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcServerPeer : RpcPeer
{
    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromMinutes(1);

    public RpcServerPeer(RpcHub hub, RpcPeerRef @ref)
        : base(hub, @ref)
        => LocalServiceFilter = static serviceDef => !serviceDef.IsBackend;

    public async Task Connect(Channel<RpcMessage> channel, CancellationToken cancellationToken = default)
    {
        Disconnect();
        using var cts = cancellationToken.LinkWith(StopToken);
        await ConnectionState.WhenDisconnected(cts.Token).ConfigureAwait(false);
        SetConnectionState(channel, null);
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken)
    {
        while (true) {
            var cts = cancellationToken.CreateLinkedTokenSource();
            try {
                var connectionState = await ConnectionState
                    .WhenConnected(cts.Token)
                    .WaitAsync(CloseTimeout, cancellationToken)
                    .ConfigureAwait(false);
                if (connectionState.Channel != null)
                    return connectionState.Channel;
            }
            finally {
                cts.CancelAndDisposeSilently();
            }
        }
    }
}
