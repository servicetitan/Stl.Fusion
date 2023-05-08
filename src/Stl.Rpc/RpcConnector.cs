using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public abstract class RpcConnector
{
    public abstract Task<Channel<RpcMessage>> Connect(RpcPeer peer, CancellationToken cancellationToken);
}
