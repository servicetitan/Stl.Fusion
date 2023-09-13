using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    // Handshake
    Task<RpcNoWait> Handshake(RpcHandshake handshake);

    // Regular calls
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
    Task<Unit> NotFound(string serviceName, string methodName);

    // Objects
    Task<RpcNoWait> KeepAlive(long[] localIds);
    Task<RpcNoWait> Disconnect(long[] localIds);

    // Streams
    Task<RpcNoWait> Ack(long nextIndex, Guid hostId = default);
    Task<RpcNoWait> I(long index, object? item);
    Task<RpcNoWait> End(long index, ExceptionInfo error);
}

public class RpcSystemCalls(IServiceProvider services)
    : RpcServiceBase(services), IRpcSystemCalls, IRpcDynamicCallHandler
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly Symbol StreamItemMethodName = nameof(I);

    public static readonly Symbol Name = "$sys";

    public Task<RpcNoWait> Handshake(RpcHandshake handshake)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        peer.ConnectionState.Value.HandshakeSource?.TrySetResult(handshake);
        return RpcNoWait.Tasks.Completed;
    }

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

    public async Task<RpcNoWait> KeepAlive(long[] localIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        await peer.SharedObjects.KeepAlive(localIds);
        return default;
    }

    public Task<RpcNoWait> Disconnect(long[] localIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        peer.RemoteObjects.Disconnect(localIds);
        return RpcNoWait.Tasks.Completed;
    }

    public async Task<RpcNoWait> Ack(long nextIndex, Guid hostId = default)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var objectId = context.Message.RelatedId;
        if (peer.SharedObjects.Get(objectId) is RpcSharedStream stream)
            await stream.OnAck(nextIndex, hostId).ConfigureAwait(false);
        else
            await peer.Hub.SystemCallSender.Disconnect(peer, new[] { objectId }).ConfigureAwait(false);
        return default;
    }

    public Task<RpcNoWait> I(long index, object? item)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var objectId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(objectId) is RpcStream stream
            ? RpcNoWait.Tasks.From(stream.OnItem(index, item))
            : RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> End(long index, ExceptionInfo error)
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
