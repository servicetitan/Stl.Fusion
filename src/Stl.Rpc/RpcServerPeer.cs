using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServerPeer : RpcPeer
{
    public static string IdPrefix { get; set; } = "@inbound-";

    public static Symbol FormatId(string clientId)
    {
        if (clientId.IsNullOrEmpty())
            throw new ArgumentOutOfRangeException(nameof(clientId));

        return IdPrefix + clientId;
    }

    public Symbol ClientId { get; init; }
    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromMinutes(1);

    public RpcServerPeer(RpcHub hub, Symbol id) : base(hub, id)
    {
        if (!id.Value.StartsWith(IdPrefix, StringComparison.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(id));

        ClientId = id.Value[IdPrefix.Length..];
        LocalServiceFilter = static serviceDef => !serviceDef.IsBackend;
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
