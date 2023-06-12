using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
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
        if (peer.Calls.Outbound.TryGetValue(outboundCallId, out var outboundCall))
            outboundCall.TryCompleteWithOk(result, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Error(ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        if (peer.Calls.Outbound.TryGetValue(outboundCallId, out var outboundCall))
            outboundCall.TryCompleteWithError(error.ToException()!, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Cancel()
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var inboundCallId = context.Message.CallId;
        if (peer.Calls.Inbound.TryGetValue(inboundCallId, out var inboundCall))
            inboundCall.Cancel();
        return RpcNoWait.Tasks.Completed;
    }

    public Type? GetArgumentListType(RpcInboundContext context)
    {
        var call = context.Call!;
        if (call.MethodDef.Method.Name == OkMethodName) {
            var outboundCallId = context.Message.CallId;
            if (!context.Peer.Calls.Outbound.TryGetValue(outboundCallId, out var outboundCall))
                return null;

            return OkMethodArgumentListTypeCache.GetOrAdd(
                outboundCall.MethodDef.UnwrappedReturnType,
                resultType => ArgumentList.Types[1].MakeGenericType(resultType));
        }
        return null;
    }
}
