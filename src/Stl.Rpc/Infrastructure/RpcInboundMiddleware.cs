namespace Stl.Rpc.Infrastructure;

public abstract class RpcInboundMiddleware(IServiceProvider services)
    : RpcMiddleware(services)
{
    public virtual Task OnBeforeCall(RpcInboundCall call)
        => Task.CompletedTask;

    public virtual Task OnAfterCall(RpcInboundCall call, Task resultTask)
        => Task.CompletedTask;
}
