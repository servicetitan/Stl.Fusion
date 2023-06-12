using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    public RpcClientChannelFactory ChannelFactory { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int ReconnectRetryLimit { get; init; } = int.MaxValue;

    public RpcClientPeer(RpcHub hub, Symbol id) : base(hub, id)
    {
        LocalServiceFilter = static _ => false;
        ChannelFactory = Hub.ClientChannelFactory;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken)
    {
        var (channel, error, tryIndex) = ConnectionState.Value;
        if (channel != null)
            return channel;

        if (error != null && Hub.UnrecoverableErrorDetector.Invoke(error, StopToken))
            throw error;
        if (tryIndex >= ReconnectRetryLimit) {
            Log.LogWarning(error, "'{PeerId}': Reconnect retry limit exceeded", Id);
            throw Errors.ConnectionUnrecoverable();
        }

        if (tryIndex == 0)
            Log.LogInformation("'{PeerId}': Connecting...", Id);
        else  {
            var delay = ReconnectDelays[tryIndex];
            Log.LogInformation(
                "'{PeerId}': Reconnecting (#{TryIndex}) after {Delay}...",
                Id, tryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        return await ChannelFactory.Invoke(this, cancellationToken).ConfigureAwait(false);
    }
}
