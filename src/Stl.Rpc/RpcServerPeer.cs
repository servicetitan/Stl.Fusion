using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServerPeer : RpcPeer
{
    public static string NamePrefix { get; set; } = "@inbound-";

    public static Symbol FormatName(string clientId)
    {
        if (clientId.IsNullOrEmpty())
            throw new ArgumentOutOfRangeException(nameof(clientId));

        return NamePrefix + clientId;
    }

    public Symbol ClientId { get; init; }
    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromMinutes(1);

    public RpcServerPeer(RpcHub hub, Symbol name) : base(hub, name)
    {
        if (!name.Value.StartsWith(NamePrefix, StringComparison.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(name));

        ClientId = name.Value[NamePrefix.Length..];
        LocalServiceFilter = static _ => true;
    }

    // Protected methods

    protected override async Task<Channel<RpcMessage>> GetChannelOrReconnect(CancellationToken cancellationToken)
    {
        while (true) {
            Exception error;
            try {
                return await GetChannel(CloseTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                error = e;
            }
            if (error is OperationCanceledException) {
                Log.LogInformation("'{Name}': Connection cancelled, shutting down", Name);
                throw error;
            }
            if (error is ImpossibleToConnectException or TimeoutException) {
                Log.LogWarning(error, "'{Name}': Can't (re)connect, shutting down", Name);
                throw error;
            }
        }
    }
}
