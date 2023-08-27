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
    Task<RpcNoWait> StreamAck(long offset);
    Task<RpcNoWait> StreamItem(object? item);
    Task<RpcNoWait> StreamEnd(ExceptionInfo? error);
}

public class RpcSystemCalls(IServiceProvider services)
    : RpcServiceBase(services), IRpcSystemCalls, IRpcDynamicCallHandler
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly Symbol StreamItemMethodName = nameof(StreamItem);

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

    public Task<RpcNoWait> StreamAck(long offset)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var streamId = context.Message.CallId;
        var streamSender = peer.OutgoingStreams.Get(streamId);
        streamSender?.OnAck(offset);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> StreamItem(object? item)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var streamId = context.Message.CallId;
        var stream = peer.IncomingStreams.Get(streamId);
        stream?.OnItem(item);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> StreamEnd(ExceptionInfo? error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var streamId = context.Message.CallId;
        var stream = peer.IncomingStreams.Get(streamId);
        stream?.OnEnd(error);
        return RpcNoWait.Tasks.Completed;
    }

    // IRpcDynamicCallHandler

    public bool IsValidCall(RpcInboundContext context, ref ArgumentList arguments, ref bool allowPolymorphism)
    {
        var call = context.Call;
        var methodName = call.MethodDef.Method.Name;
        if (methodName == OkMethodName) {
            var outboundCall = context.Peer.OutboundCalls.Get(context.Message.CallId);
            if (outboundCall == null)
                return false;

            var outboundMethodDef = outboundCall.MethodDef;
            arguments = outboundMethodDef.ResultListFactory.Invoke();
            allowPolymorphism = outboundMethodDef.AllowResultPolymorphism;
            return true;
        }
        if (methodName == StreamItemMethodName) {
            var stream = context.Peer.IncomingStreams.Get(context.Message.CallId);
            if (stream == null)
                return false;

            arguments = stream.CreateStreamItemArguments();
            allowPolymorphism = true;
            return true;
        }
        return false;
    }
}
