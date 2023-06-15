using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcInbound404Call<TResult> : RpcInboundCall<TResult>
{
    public RpcInbound404Call(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override Task Invoke()
    {
        var message = Context.Message;
        var (service, method) = (message.Service, message.Method);
        var error = Errors.EndpointNotFound(service, method);
        Arguments = ArgumentList.New(service, method);
        Result = new Result<TResult>(default!, error);
        var systemCallSender = Hub.SystemCallSender;
        return systemCallSender.Error(Context.Peer, Id, error, ResultHeaders).AsTask();
    }
}
