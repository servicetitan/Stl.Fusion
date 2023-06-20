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

public sealed class RpcInboundMiddlewares : RpcMiddlewares<RpcInboundMiddleware>
{
    public RpcInboundMiddlewares(IServiceProvider services) : base(services) { }
}

public sealed class RpcOutboundMiddlewares : RpcMiddlewares<RpcOutboundMiddleware>
{
    public RpcOutboundMiddlewares(IServiceProvider services) : base(services) { }

    public void PrepareCall(RpcOutboundContext context)
    {
        foreach (var m in Instances)
            m.PrepareCall(context);
    }
}
