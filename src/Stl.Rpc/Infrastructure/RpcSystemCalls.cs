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

    // Objects
    Task<RpcNoWait> KeepAlive(long[] objectIds);
    Task<RpcNoWait> MissingObjects(long[] objectIds);

    // Streams
    Task<RpcNoWait> StreamAck(long nextIndex, bool mustReset);
    Task<RpcNoWait> StreamItem(long index, long ackIndex, object? item);
    Task<RpcNoWait> StreamEnd(long index, ExceptionInfo error);
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
        var outboundCallId = context.Message.RelatedId;
        peer.OutboundCalls.Get(outboundCallId)?.SetResult(result, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Error(ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.RelatedId;
        peer.OutboundCalls.Get(outboundCallId)?.SetError(error.ToException()!, context);
        return RpcNoWait.Tasks.Completed;
    }

    public async Task<RpcNoWait> Cancel()
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var inboundCallId = context.Message.RelatedId;
        var inboundCall = peer.InboundCalls.Get(inboundCallId);
        if (inboundCall != null)
            await inboundCall.Complete(silentCancel: true).ConfigureAwait(false);
        return default;
    }

    public Task<Unit> NotFound(string serviceName, string methodName)
        => throw Errors.EndpointNotFound(serviceName, methodName);

    public Task<RpcNoWait> KeepAlive(long[] objectIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        peer.SharedObjects.OnKeepAlive(objectIds);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> MissingObjects(long[] objectIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        peer.RemoteObjects.MissingObjects(objectIds);
        return RpcNoWait.Tasks.Completed;
    }

    public async Task<RpcNoWait> StreamAck(long nextIndex, bool mustReset)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var objectId = context.Message.RelatedId;
        if (peer.SharedObjects.Get(objectId) is RpcSharedStream stream)
            stream.OnAck(nextIndex, mustReset);
        else
            await peer.Hub.SystemCallSender.MissingObjects(peer, new[] { objectId }).ConfigureAwait(false);
        return default;
    }

    public Task<RpcNoWait> StreamItem(long index, long ackIndex, object? item)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var objectId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(objectId) is RpcStream stream
            ? RpcNoWait.Tasks.From(stream.OnItem(index, ackIndex, item))
            : RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> StreamEnd(long index, ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var objectId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(objectId) is RpcStream stream
            ? RpcNoWait.Tasks.From(stream.OnEnd(index, error.IsNone ? null : error.ToException()))
            : RpcNoWait.Tasks.Completed;
    }

    // IRpcDynamicCallHandler

    public bool IsValidCall(RpcInboundContext context, ref ArgumentList arguments, ref bool allowPolymorphism)
    {
        var call = context.Call;
        var methodName = call.MethodDef.Method.Name;
        if (methodName == OkMethodName) {
            var outboundCall = context.Peer.OutboundCalls.Get(context.Message.RelatedId);
            if (outboundCall == null)
                return false;

            var outboundMethodDef = outboundCall.MethodDef;
            arguments = outboundMethodDef.ResultListFactory.Invoke();
            allowPolymorphism = outboundMethodDef.AllowResultPolymorphism;
            return true;
        }
        if (methodName == StreamItemMethodName) {
            var stream = context.Peer.RemoteObjects.Get(context.Message.RelatedId) as RpcStream;
            if (stream == null)
                return false;

            arguments = stream.CreateStreamItemArguments();
            allowPolymorphism = true;
            return true;
        }
        return false;
    }
}
