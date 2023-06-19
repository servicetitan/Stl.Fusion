namespace Stl.Rpc.Infrastructure;

public static class RpcPeerConnectionStateExt
{
    public static Task<RpcPeerConnectionState> WhenConnected(this AsyncEvent<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
        => connectionState.When(static s => s.IsConnected(), cancellationToken);

    public static Task<RpcPeerConnectionState> WhenDisconnected(this AsyncEvent<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
        => connectionState.When(static s => !s.IsConnected(), cancellationToken);

    public static async IAsyncEnumerable<bool> IsConnectedChanges(this AsyncEvent<RpcPeerConnectionState> connectionState,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var isConnected = connectionState.Value.IsConnected();
        var lastIsConnected = !isConnected;
        while (true) {
            if (isConnected != lastIsConnected) {
                yield return isConnected;
                lastIsConnected = isConnected;
            }
            await connectionState.WhenNext(cancellationToken).ConfigureAwait(false);
            isConnected = connectionState.Value.IsConnected();
        }
        // ReSharper disable once IteratorNeverReturns
    }
}
