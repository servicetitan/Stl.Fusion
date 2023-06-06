using Stl.Channels;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcClientPeer : RpcPeer
{
    public RpcClientChannelProvider ChannelProvider { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int ReconnectRetryLimit { get; init; } = int.MaxValue;

    public RpcClientPeer(RpcHub hub, Symbol name) : base(hub, name)
    {
        LocalServiceFilter = static _ => false;
        ChannelProvider = Hub.ClientChannelProvider;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken)
    {
        var (channel, error, tryIndex) = GetConnectionState();
        if (channel != null)
            return channel;

        if (error is OperationCanceledException) {
            Log.LogInformation("'{Name}': Connection cancelled, shutting down", Name);
            throw error;
        }
        if (error is ImpossibleToConnectException or TimeoutException) {
            Log.LogWarning(error, "'{Name}': Can't (re)connect, shutting down", Name);
            throw error;
        }
        if (tryIndex >= ReconnectRetryLimit) {
            Log.LogWarning(error, "'{Name}': Reconnect retry limit exceeded", Name);
            throw Errors.ImpossibleToReconnect();
        }

        if (tryIndex == 0)
            Log.LogInformation("'{Name}': Connecting...", Name);
        else  {
            var delay = ReconnectDelays[tryIndex];
            Log.LogInformation(
                "'{Name}': Reconnecting (#{TryIndex}) after {Delay}...",
                Name, tryIndex, delay.ToShortString());
            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        try {
            channel = await ChannelProvider.Invoke(this, cancellationToken).ConfigureAwait(false);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (channel is null or IEmptyChannel)
                throw Errors.ImpossibleToReconnect();

            SetConnectionState(channel);
            return channel;
        }
        catch (Exception e) {
            SetConnectionState(null, e);
            throw;
        }
    }
}
