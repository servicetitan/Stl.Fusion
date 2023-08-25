using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    // Regular calls
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
    Task<Unit> NotFound(string serviceName, string methodName);

    // Streams
    Task<RpcNoWait> GetStream(RpcStreamId streamId, long skipTo, CancellationToken cancellationToken);
    Task<RpcNoWait> StreamStart(TypeRef itemTypeRef);
    Task<RpcNoWait> StreamItem(object? item);
    Task<RpcNoWait> StreamEnd(ExceptionInfo? error);
}

public class RpcSystemCalls(IServiceProvider services)
    : RpcServiceBase(services), IRpcSystemCalls, IRpcDynamicCallHandler
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly Symbol ItemMethodName = nameof(StreamItem);

    public static readonly Symbol Name = "$sys";

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

    public Task<RpcNoWait> GetStream(RpcStreamId streamId, long skipTo, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<RpcNoWait> StreamStart(TypeRef itemTypeRef)
    {
        throw new NotImplementedException();
    }

    public Task<RpcNoWait> StreamItem(object? item)
    {
        throw new NotImplementedException();
    }

    public Task<RpcNoWait> StreamEnd(ExceptionInfo? error)
    {
        throw new NotImplementedException();
    }


    // IRpcDynamicCallHandler

    public bool IsValidCall(RpcInboundContext context, ref ArgumentList arguments, ref bool allowPolymorphism)
    {
        var call = context.Call;
        var outboundCall = context.Peer.OutboundCalls.Get(context.Message.CallId);
        if (outboundCall == null)
            return false;

        var methodName = call.MethodDef.Method.Name;
        if (methodName == OkMethodName) {
            var outboundMethodDef = outboundCall.MethodDef;
            arguments = outboundMethodDef.ResultListFactory.Invoke();
            allowPolymorphism = outboundMethodDef.AllowResultPolymorphism;
            return true;
        }
        if (methodName == ItemMethodName && outboundCall is IRpcOutboundStreamCall outboundStreamCall) {
            arguments = outboundStreamCall.ItemResultListFactory.Invoke();
            allowPolymorphism = true;
            return true;
        }
        return false;
    }
}
