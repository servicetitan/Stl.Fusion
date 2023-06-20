namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddleware : RpcServiceBase
{
    public double Priority { get; set; }

    protected RpcMiddleware(IServiceProvider services) : base(services) { }
}

public abstract class RpcInboundMiddleware : RpcMiddleware
{
    // To be done

    protected RpcInboundMiddleware(IServiceProvider services) : base(services) { }
}

public abstract class RpcOutboundMiddleware : RpcMiddleware
{
    protected RpcOutboundMiddleware(IServiceProvider services) : base(services) { }

    public abstract void PrepareCall(RpcOutboundContext context);
}
