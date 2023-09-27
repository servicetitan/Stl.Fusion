namespace Stl.Rpc.Infrastructure;

public static class RpcPeerConnectionStateExt
{
    public static Task<AsyncState<RpcPeerConnectionState>> WhenConnected(
        this AsyncState<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
        => connectionState.When(static s => s.IsConnected(), cancellationToken);

    public static Task<AsyncState<RpcPeerConnectionState>> WhenDisconnected(
        this AsyncState<RpcPeerConnectionState> connectionState,
        CancellationToken cancellationToken = default)
        => connectionState.When(static s => !s.IsConnected(), cancellationToken);
}
