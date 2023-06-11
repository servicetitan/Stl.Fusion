namespace Stl.Rpc;

public static class RpcPeerFactoryExt
{
    public static RpcPeerFactory Default(RpcHub rpcHub)
        => name => name.Value.StartsWith(RpcServerPeer.IdPrefix, StringComparison.Ordinal)
            ? new RpcServerPeer(rpcHub, name)
            : new RpcClientPeer(rpcHub, name);
}
