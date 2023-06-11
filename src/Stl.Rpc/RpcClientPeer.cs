using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    public RpcClientChannelProvider ChannelProvider { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int ReconnectRetryLimit { get; init; } = int.MaxValue;

    public RpcClientPeer(RpcHub hub, Symbol id) : base(hub, id)
    {
        LocalServiceFilter = static _ => false;
        ChannelProvider = Hub.ClientChannelProvider;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken)
    {
        var (channel, error, tryIndex) = ConnectionState.Value;
        if (channel != null)
            return channel;

        if (error != null && Hub.ErrorClassifier.IsUnrecoverableError(error))
            throw error;
        if (tryIndex >= ReconnectRetryLimit) {
            Log.LogWarning(error, "'{Name}': Reconnect retry limit exceeded", Id);
            throw Errors.ConnectionUnrecoverable();
        }

        if (tryIndex == 0)
            Log.LogInformation("'{Name}': Connecting...", Id);
        else  {
            var delay = ReconnectDelays[tryIndex];
            Log.LogInformation(
                "'{Name}': Reconnecting (#{TryIndex}) after {Delay}...",
                Id, tryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        try {
            return await ChannelProvider.Invoke(this, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            Log.LogInformation("'{Name}': Connection cancelled", Id);
            throw;
        }
    }
}
