using Stl.Rpc.Internal;

namespace Stl.Rpc;

public static class RpcPeerRefExt
{
    public static TPeerRef RequireClient<TPeerRef>(this TPeerRef peerRef)
        where TPeerRef : RpcPeerRef
        => !peerRef.IsServer
            ? peerRef
            : throw Errors.ClientRpcPeerRefExpected(nameof(peerRef));

    public static TPeerRef RequireServer<TPeerRef>(this TPeerRef peerRef)
        where TPeerRef : RpcPeerRef
        => peerRef.IsServer
            ? peerRef
            : throw Errors.ServerRpcPeerRefExpected(nameof(peerRef));
}
