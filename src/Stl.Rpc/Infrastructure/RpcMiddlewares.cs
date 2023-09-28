namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddlewares<TMiddleware>
    where TMiddleware : RpcMiddleware
{
    public readonly TMiddleware[] Instances;
    public readonly bool HasInstances;

    protected RpcMiddlewares(IServiceProvider services)
    {
        var instances = services.GetRequiredService<IEnumerable<TMiddleware>>();
        Instances = instances.OrderByDescending(x => x.Priority).ToArray();
        HasInstances = Instances.Length != 0;
    }
}
