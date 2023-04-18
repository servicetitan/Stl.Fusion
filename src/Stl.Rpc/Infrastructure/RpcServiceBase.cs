namespace Stl.Rpc.Infrastructure;

public abstract class RpcServiceBase
{
    protected IServiceProvider Services { get; }
    protected ILogger Log { get; }
    protected RpcGlobalOptions GlobalOptions { get; }

    protected RpcServiceBase(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
        GlobalOptions = services.GetRequiredService<RpcGlobalOptions>();
    }
}
