namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddleware
{
    protected IServiceProvider Services { get; }
    protected ILogger Log { get; }

    protected RpcMiddleware(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
    }

    public abstract Task Invoke(RpcContext context, CancellationToken cancellationToken);
}
