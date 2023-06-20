using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInbound404Call<TResult> : RpcInboundCall<TResult>
{
    public RpcInbound404Call(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    protected override Task<TResult> InvokeTarget()
    {
        var message = Context.Message;
        var (service, method) = (message.Service, message.Method);
        return Task.FromException<TResult>(Errors.EndpointNotFound(service, method));
    }
}
