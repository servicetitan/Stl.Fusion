namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddleware : RpcServiceBase
{
    public double Priority { get; set; }

    protected RpcMiddleware(IServiceProvider services) : base(services) { }
}
