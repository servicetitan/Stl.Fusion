namespace Stl.Rpc;

public record RpcPeerRef(Symbol Id, bool IsServer = false)
{
    public static RpcPeerRef Default { get; set; } = NewClient("default");

    public static RpcPeerRef NewServer(Symbol id)
        => new(id, true);
    public static RpcPeerRef NewClient(Symbol id)
        => new(id);

    public override string ToString()
        => $"{(IsServer ? "server" : "client")}:{Id}";

    // Operators

    public static implicit operator RpcPeerRef(RpcPeer peer) => peer.Ref;
}
