namespace Stl.Rpc.Infrastructure;

public abstract class RpcServiceBase : IHasServices
{
    private RpcHub? _hub;
    private ILogger? _log;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }
    public RpcHub Hub => _hub ??= Services.RpcHub();

    protected RpcServiceBase(IServiceProvider services)
        => Services = services;
}
