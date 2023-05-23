namespace Stl.Rpc.Infrastructure;

public class RpcCallRegistry
{
    private long _lastId;

    public RpcPeer Peer { get; }
    public long NextId => Interlocked.Increment(ref _lastId);
    public ConcurrentDictionary<long, RpcInboundCall> Inbound { get; } = new();
    public ConcurrentDictionary<long, RpcOutboundCall> Outbound { get; } = new();

    public RpcCallRegistry(RpcPeer peer)
        => Peer = peer;
}
