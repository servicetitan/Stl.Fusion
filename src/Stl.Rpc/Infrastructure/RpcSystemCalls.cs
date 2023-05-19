using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcSystemCalls : RpcServiceBase, IRpcSystemCalls, IRpcCallValidator
{
    private static readonly Symbol ResultMethodName = nameof(Result);

    public static readonly Symbol Name = "$sys";

    public RpcSystemCalls(IServiceProvider services) : base(services)
    { }

    public Task<RpcNoWait> Result(object? result)
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        outboundCall.Complete(result);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Error(ExceptionInfo error)
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        outboundCall.Complete(error);
        return RpcNoWait.Tasks.Completed;
    }

    public void ValidateCall(RpcInboundContext context, Type[] argumentTypes)
    {
        var call = context.Call!;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        if (call.MethodDef.Name == ResultMethodName) {
            var expectedResultType = outboundCall.MethodDef.UnwrappedReturnType;
            var actualResultType = argumentTypes[1];
            if (!expectedResultType.IsAssignableFrom(actualResultType))
                throw Errors.IncompatibleResultType(call.MethodDef, actualResultType);
        }
    }
}
