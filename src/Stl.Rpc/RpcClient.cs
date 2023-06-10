using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public abstract class RpcClient : IHasServices
{
    private ILogger? _log;
    private RpcHub? _rpcHub;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }
    public RpcHub RpcHub => _rpcHub ??= Services.GetRequiredService<RpcHub>();
    public Symbol ClientId { get; init; } = default;

    protected RpcClient(IServiceProvider services)
        => Services = services;

    public abstract Task<Channel<RpcMessage>> GetChannel(RpcClientPeer peer, CancellationToken cancellationToken);
}
