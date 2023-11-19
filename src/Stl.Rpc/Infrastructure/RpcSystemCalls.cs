using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Internal;
using Errors = Stl.Rpc.Internal.Errors;

namespace Stl.Rpc.Infrastructure;

#pragma warning disable IL2046

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
    Task<RpcNoWait> B(long index, object? items);
    Task<RpcNoWait> End(long index, ExceptionInfo error);
}

public class RpcSystemCalls(IServiceProvider services)
    : RpcServiceBase(services), IRpcSystemCalls, IRpcDynamicCallHandler
{
    private static readonly Symbol OkMethodName = nameof(Ok);
    private static readonly Symbol ItemMethodName = nameof(I);
    private static readonly Symbol BatchMethodName = nameof(B);

    public static readonly Symbol Name = "$sys";

    public Task<RpcNoWait> Handshake(RpcHandshake handshake)
        => RpcNoWait.Tasks.Completed; // Does nothing: this call is processed inside RpcPeer.OnRun

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task<RpcNoWait> Ok(object? result)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.RelatedId;
        peer.OutboundCalls.Get(outboundCallId)?.SetResult(result, context);
        return RpcNoWait.Tasks.Completed;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task<RpcNoWait> Error(ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.RelatedId;
        peer.OutboundCalls.Get(outboundCallId)?.SetError(error.ToException()!, context);
        return RpcNoWait.Tasks.Completed;
    }

    public Task<RpcNoWait> Cancel()
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var inboundCallId = context.Message.RelatedId;
        var inboundCall = peer.InboundCalls.Get(inboundCallId);
        if (inboundCall != null) {
            peer.Log.IfEnabled(LogLevel.Debug)
                ?.LogDebug("Remote call cancelled on the client side: {Call}", inboundCallId);
            inboundCall.Cancel();
        }
        return RpcNoWait.Tasks.Completed;
    }

    public Task<Unit> NotFound(string serviceName, string methodName)
        => throw Errors.EndpointNotFound(serviceName, methodName);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public async Task<RpcNoWait> KeepAlive(long[] localIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        await peer.SharedObjects.KeepAlive(localIds).ConfigureAwait(false);
        return default;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task<RpcNoWait> Disconnect(long[] localIds)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        peer.RemoteObjects.Disconnect(localIds);
        return RpcNoWait.Tasks.Completed;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public async Task<RpcNoWait> Ack(long nextIndex, Guid hostId = default)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var localId = context.Message.RelatedId;
        if (peer.SharedObjects.Get(localId) is RpcSharedStream stream)
            await stream.OnAck(nextIndex, hostId).ConfigureAwait(false);
        else
            await peer.Hub.SystemCallSender.Disconnect(peer, new[] { localId }).ConfigureAwait(false);
        return default;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task<RpcNoWait> I(long index, object? item)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var localId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(localId) is RpcStream stream
            ? RpcNoWait.Tasks.From(stream.OnItem(index, item))
            : RpcNoWait.Tasks.Completed;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task<RpcNoWait> B(long index, object? items)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var localId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(localId) is RpcStream stream
            ? RpcNoWait.Tasks.From(stream.OnBatch(index, items))
            : RpcNoWait.Tasks.Completed;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public Task<RpcNoWait> End(long index, ExceptionInfo error)
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var localId = context.Message.RelatedId;
        return peer.RemoteObjects.Get(localId) is RpcStream stream
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
        if (methodName == ItemMethodName) {
            var stream = context.Peer.RemoteObjects.Get(context.Message.RelatedId) as RpcStream;
            if (stream == null)
                return false;

            arguments = stream.CreateStreamItemArguments();
            allowPolymorphism = true;
            return true;
        }
        if (methodName == BatchMethodName) {
            var stream = context.Peer.RemoteObjects.Get(context.Message.RelatedId) as RpcStream;
            if (stream == null)
                return false;

            arguments = stream.CreateStreamBatchArguments();
            allowPolymorphism = true;
            return true;
        }
        return false;
    }
}
