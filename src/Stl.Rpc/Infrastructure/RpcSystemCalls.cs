using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcSystemCalls : RpcServiceBase, IRpcSystemCalls, IRpcArgumentTypeResolver
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly Symbol FailMethodName = nameof(Fail);
    private static readonly Symbol CancelMethodName = nameof(Cancel);

    public static readonly Symbol Name = "$sys";

    public RpcSystemCalls(IServiceProvider services) : base(services)
    { }

    public Task<RpcNoWait> Ok(object? result)
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        outboundCall.Complete(result);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Fail(ExceptionInfo error)
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        outboundCall.Complete(error);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Cancel()
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var inboundCallId = context.Message.CallId;
        var inboundCall = peer.Calls.Inbound[inboundCallId];
        inboundCall.CancellationTokenSource.CancelAndDisposeSilently();
        return RpcNoWait.Tasks.Completed;
    }

    public void ResolveArgumentTypes(RpcInboundContext context, Type[] argumentTypes)
    {
        var call = context.Call!;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        var outboundCall = peer.Calls.Outbound[outboundCallId];
        if (call.MethodDef.Name == OkMethodName) {
            var expectedResultType = outboundCall.MethodDef.UnwrappedReturnType;
            var actualResultType = argumentTypes[1];
            if (!expectedResultType.IsAssignableFrom(actualResultType))
                throw Errors.IncompatibleResultType(call.MethodDef, actualResultType);
        }
    }
}
