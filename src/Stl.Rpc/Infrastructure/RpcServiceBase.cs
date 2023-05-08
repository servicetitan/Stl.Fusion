namespace Stl.Rpc.Infrastructure;

public abstract class RpcServiceBase
{
    private RpcHub? _hub;
    private ILogger? _log;

    protected IServiceProvider Services { get; }
    protected RpcHub Hub => _hub ??= Services.RpcHub();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    protected RpcServiceBase(IServiceProvider services)
        => Services = services;
}
