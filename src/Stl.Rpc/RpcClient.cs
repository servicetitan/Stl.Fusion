using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public abstract class RpcClient : IHasServices
{
    private ILogger? _log;
    private RpcHub? _rpcHub;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }
    public RpcHub RpcHub { get; }
    public string ClientId { get; init; }

    protected RpcClient(IServiceProvider services)
    {
        Services = services;
        RpcHub = services.RpcHub();
        ClientId = RpcHub.ClientIdGenerator.Invoke();
    }

    public abstract Task<Channel<RpcMessage>> GetChannel(RpcClientPeer peer, CancellationToken cancellationToken);
}
