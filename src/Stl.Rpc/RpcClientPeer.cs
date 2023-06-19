using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    public RpcClientChannelFactory ChannelFactory { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int? ReconnectRetryLimit { get; init; }

    public RpcClientPeer(RpcHub hub, RpcPeerRef @ref)
        : base(hub, @ref)
    {
        LocalServiceFilter = static _ => false;
        ChannelFactory = Hub.ClientChannelFactory;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannel(CancellationToken cancellationToken)
    {
        var (channel, error, _, tryIndex) = ConnectionState.ThrowIfTerminal().Value;
        if (channel != null)
            return channel;

        if (ReconnectRetryLimit is { } limit && tryIndex >= limit) {
            Log.LogWarning(error, "'{PeerId}': Reconnect retry limit exceeded", Ref);
            throw Errors.ConnectionUnrecoverable();
        }

        if (tryIndex == 0)
            Log.LogInformation("'{PeerId}': Connecting...", Ref);
        else  {
            var delay = ReconnectDelays[tryIndex];
            delay = TimeSpanExt.Max(TimeSpan.FromMilliseconds(1), delay);
            Log.LogInformation(
                "'{PeerId}': Reconnecting (#{TryIndex}) after {Delay}...",
                Ref, tryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        return await ChannelFactory.Invoke(this, cancellationToken).ConfigureAwait(false);
    }
}
