namespace Stl.Rpc;

public record RpcPeerRef(Symbol Key, bool IsServer = false)
{
    public static RpcPeerRef Default { get; set; } = NewClient("default");

    public static RpcPeerRef NewServer(Symbol key)
        => new(key, true);
    public static RpcPeerRef NewClient(Symbol key)
        => new(key);

    public override string ToString()
        => $"{(IsServer ? "server" : "client")}:{Key}";

    // Operators

    public static implicit operator RpcPeerRef(RpcPeer peer) => peer.Ref;
}
