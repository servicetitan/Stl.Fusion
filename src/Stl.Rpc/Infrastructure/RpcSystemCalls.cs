using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
    Task<Unit> NotFound(string serviceName, string methodName);
}

public class RpcSystemCalls : RpcServiceBase, IRpcSystemCalls, IRpcArgumentListTypeResolver
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly ConcurrentDictionary<Type, Type> OkMethodArgumentListTypeCache = new();

    public static readonly Symbol Name = "$sys";

    public RpcSystemCalls(IServiceProvider services) : base(services)
    { }

    public Task<RpcNoWait> Ok(object? result)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        peer.OutboundCalls.Get(outboundCallId)?.SetResult(result, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Error(ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        peer.OutboundCalls.Get(outboundCallId)?.SetError(error.ToException()!, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Cancel()
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var inboundCallId = context.Message.CallId;
        var inboundCall = peer.InboundCalls.Get(inboundCallId);
        if (inboundCall != null)
            _ = inboundCall.Complete(silentCancel: true);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<Unit> NotFound(string serviceName, string methodName)
        => throw Errors.EndpointNotFound(serviceName, methodName);

    public Type? GetArgumentListType(RpcInboundContext context)
    {
        var call = context.Call;
        if (call.MethodDef.Method.Name == OkMethodName) {
            var outboundCallId = context.Message.CallId;
            var outboundCall = context.Peer.OutboundCalls.Get(outboundCallId);
            if (outboundCall == null)
                return null;

            return OkMethodArgumentListTypeCache.GetOrAdd(
                outboundCall.MethodDef.UnwrappedReturnType,
                resultType => ArgumentList.Types[1].MakeGenericType(resultType));
        }
        return null;
    }
}
