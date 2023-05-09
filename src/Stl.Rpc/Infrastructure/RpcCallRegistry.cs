namespace Stl.Rpc.Infrastructure;

public class RpcCallRegistry
{
    private long _lastId;

    public RpcPeer Peer { get; }
    public long NextId => Interlocked.Increment(ref _lastId);
    public ConcurrentDictionary<long, IRpcInboundCall> Inbound { get; } = new();
    public ConcurrentDictionary<long, IRpcOutboundCall> Outbound { get; } = new();

    public RpcCallRegistry(RpcPeer peer)
        => Peer = peer;
}
