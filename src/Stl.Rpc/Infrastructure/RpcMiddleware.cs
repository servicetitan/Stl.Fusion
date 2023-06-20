namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddleware
{
    public double Priority { get; set; }
}

public abstract class RpcInboundMiddleware : RpcMiddleware
{ }

public abstract class RpcOutboundMiddleware : RpcMiddleware
{
    public abstract void PrepareCall(RpcOutboundContext context);
}
