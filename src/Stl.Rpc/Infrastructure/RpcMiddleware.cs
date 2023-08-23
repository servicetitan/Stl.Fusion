namespace Stl.Rpc.Infrastructure;

public abstract class RpcMiddleware(IServiceProvider services)
    : RpcServiceBase(services)
{
    public double Priority { get; set; }
}
