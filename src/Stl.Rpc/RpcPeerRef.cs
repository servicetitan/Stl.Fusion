namespace Stl.Rpc;

public record RpcPeerRef(Type PeerType, Symbol Id)
{
    public static RpcPeerRef Default { get; set; } = NewClient("default");

    public static RpcPeerRef NewServer(Symbol id)
        => new(typeof(RpcServerPeer), id);
    public static RpcPeerRef NewClient(Symbol id)
        => new(typeof(RpcClientPeer), id);

    public override string ToString()
        => $"{PeerType.Name}:{Id}";
}
