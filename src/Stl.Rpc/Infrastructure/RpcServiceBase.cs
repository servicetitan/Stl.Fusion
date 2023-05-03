namespace Stl.Rpc.Infrastructure;

public abstract class RpcServiceBase
{
    protected IServiceProvider Services { get; }
    protected ILogger Log { get; }
    protected RpcConfiguration Configuration { get; }

    protected RpcServiceBase(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
        Configuration = services.GetRequiredService<RpcConfiguration>();
    }
}
