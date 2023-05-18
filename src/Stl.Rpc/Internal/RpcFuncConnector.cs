using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public class RpcFuncConnector : RpcConnector
{
    private readonly Func<RpcPeer, CancellationToken, Task<Channel<RpcMessage>>> _connector;

    public RpcFuncConnector(Func<RpcPeer, CancellationToken, Task<Channel<RpcMessage>>> connector)
        => _connector = connector;

    public override Task<Channel<RpcMessage>> Connect(RpcPeer peer, CancellationToken cancellationToken)
        => _connector.Invoke(peer, cancellationToken);
}
