using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public abstract class RpcClient : RpcServiceBase
{
    public string ClientId { get; init; }

    protected RpcClient(IServiceProvider services)
        : base(services)
        => ClientId = Hub.ClientIdGenerator.Invoke();

    public abstract Task<RpcConnection> CreateConnection(RpcClientPeer peer, CancellationToken cancellationToken);
}
