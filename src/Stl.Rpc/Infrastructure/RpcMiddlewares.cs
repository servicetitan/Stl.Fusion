namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddlewares<TMiddleware>
    where TMiddleware : RpcMiddleware
{
    public TMiddleware[] Instances { get; }

    protected RpcMiddlewares(IServiceProvider services)
    {
        var instances = services.GetRequiredService<IEnumerable<TMiddleware>>();
        Instances = instances.OrderByDescending(x => x.Priority).ToArray();
    }
}
