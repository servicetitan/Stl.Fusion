namespace Stl.Rpc.Infrastructure;

public class RpcInboundHandler : RpcServiceBase
{
    public RpcInboundHandler(IServiceProvider services) : base(services) { }

    public virtual Task Handle(RpcInboundContext context)
        => context.Call!.Invoke();
}
