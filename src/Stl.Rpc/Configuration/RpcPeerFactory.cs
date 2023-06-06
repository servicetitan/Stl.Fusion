namespace Stl.Rpc;

public delegate RpcPeer RpcPeerFactory(Symbol name);

public static class RpcPeerFactoryExt
{
    public static RpcPeerFactory Default(RpcHub rpcHub)
        => name => name.Value.StartsWith(RpcServerPeer.NamePrefix, StringComparison.Ordinal)
            ? new RpcServerPeer(rpcHub, name)
            : new RpcClientPeer(rpcHub, name);
}
