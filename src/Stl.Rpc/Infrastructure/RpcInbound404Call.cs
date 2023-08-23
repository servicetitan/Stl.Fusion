using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInbound404Call<TResult>(RpcInboundContext context, RpcMethodDef methodDef)
    : RpcInboundCall<TResult>(context, methodDef)
{
    protected override Task<TResult> InvokeTarget()
    {
        var message = Context.Message;
        var (service, method) = (message.Service, message.Method);
        return Task.FromException<TResult>(Errors.EndpointNotFound(service, method));
    }
}
