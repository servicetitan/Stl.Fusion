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
        await ConnectionState.When(s => s.Channel == null, cts.Token).ConfigureAwait(false);
        SetConnectionState(channel, null, false);
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var connectionState = ConnectionState;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNextConnectionState = connectionState.WhenNext();
            if (!whenNextConnectionState.IsCompleted) {
                var (channel, error, _) = connectionState.Value;
                if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken))
                    throw error;

                if (channel != null)
                    return channel;
            }

            connectionState = await whenNextConnectionState.WaitAsync(CloseTimeout, cancellationToken).ConfigureAwait(false);
        }
    }
}
