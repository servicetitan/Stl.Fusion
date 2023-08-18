namespace Stl.Rpc.Infrastructure;

public static class RpcPeerConnectionStateExt
{
    public static Task<RpcPeerConnectionState> WhenConnected(this AsyncEvent<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
        => connectionState.When(static s => s.IsConnected(), cancellationToken);

    public static async Task<RpcPeerConnectionState> WhenDisconnected(this AsyncEvent<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
    {
        try {
            return await connectionState
                .When(static s => !s.IsConnected(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AsyncEventSequenceCompletedException) {
            return connectionState.LatestOrLastIfCompleted().Value;
        }
        catch (ConnectionUnrecoverableException) {
            return connectionState.LatestOrLastIfCompleted().Value;
        }
    }

    public static async IAsyncEnumerable<bool> IsConnectedChanges(this AsyncEvent<RpcPeerConnectionState>? connectionState,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var isConnected = connectionState?.Value.IsConnected() ?? false;
        var lastIsConnected = !isConnected;
        while (connectionState != null) {
            if (isConnected != lastIsConnected) {
                yield return isConnected;
                lastIsConnected = isConnected;
            }
            try {
                connectionState = await connectionState.WhenNext(cancellationToken).ConfigureAwait(false);
                isConnected = connectionState!.Value.IsConnected();
            }
            catch (ConnectionUnrecoverableException) {
                connectionState = null;
                isConnected = false;
            }
        }
        if (isConnected != lastIsConnected)
            yield return isConnected;
    }
}
