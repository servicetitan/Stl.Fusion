using Stl.Interception;
using Stl.Rpc.Caching;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcOutboundContext, RpcOutboundCall>> FactoryCache = new();

    public readonly RpcOutboundContext Context;
    public readonly RpcPeer Peer;
    public RpcMessage? Message;
    public Task ResultTask { get; protected init; } = null!;
    public int ConnectTimeoutMs;
    public int TimeoutMs;

    public static RpcOutboundCall New(RpcOutboundContext context)
        => FactoryCache.GetOrAdd((context.CallTypeId, context.MethodDef!.UnwrappedReturnType), static key => {
            var (callTypeId, tResult) = key;
            var type = RpcCallTypeRegistry.Resolve(callTypeId)
                .OutboundCallType
                .MakeGenericType(tResult);
            return (Func<RpcOutboundContext, RpcOutboundCall>)type.GetConstructorDelegate(typeof(RpcOutboundContext))!;
        }).Invoke(context);

    protected RpcOutboundCall(RpcOutboundContext context)
        : base(context.MethodDef!)
    {
        Context = context;
        Peer = context.Peer!; // Calls
    }

    public override string ToString()
    {
        var context = Context;
        var headers = context.Headers.OrEmpty();
        var arguments = context.Arguments;
        var methodDef = context.MethodDef;
        var ctIndex = methodDef?.CancellationTokenIndex ?? -1;
        if (ctIndex >= 0)
            arguments = arguments?.Remove(ctIndex);
        return $"{GetType().GetName()} #{Id}: {methodDef?.Name ?? "n/a"}{arguments?.ToString() ?? "(n/a)"}"
            + (headers.Count > 0 ? $", Headers: {headers.ToDelimitedString()}" : "");
    }

    public ValueTask RegisterAndSend()
    {
        if (NoWait)
            return SendNoWait(MethodDef.AllowArgumentPolymorphism);

        Peer.OutboundCalls.Register(this);
        var sendTask = SendRegistered();

        // RegisterCancellationHandler must follow SendRegistered,
        // coz it's possible that ResultTask is already completed
        // at this point (e.g. due to an error), and thus
        // cancellation handler isn't necessary.
        if (!ResultTask.IsCompleted)
            RegisterCancellationHandler();
        return sendTask;
    }

    public ValueTask SendNoWait(bool allowPolymorphism)
    {
        var message = CreateMessage(Context.RelatedCallId, allowPolymorphism);
        Peer.CallLog?.Log(Peer.CallLogLevel, "'{PeerRef}': -> {Call}", Peer.Ref, this);
        return Peer.Send(message);
    }

    public ValueTask SendRegistered(bool notifyCancelled = false)
    {
        RpcMessage message;
        try {
            message = CreateMessage(Id, MethodDef.AllowArgumentPolymorphism);
            if (Context.CacheInfoCapture is { Key: null } cacheInfoCapture) {
                cacheInfoCapture.Key = new RpcCacheKey(MethodDef.Service.Name, MethodDef.Name, message.ArgumentData);
                if (!cacheInfoCapture.MustCaptureResult) {
                    SetResult(default, null);
                    return default;
                }
            }
        }
        catch (Exception error) {
            SetError(error, null, notifyCancelled);
            return default;
        }
        Peer.CallLog?.Log(Peer.CallLogLevel, "'{PeerRef}': -> {Call}", Peer.Ref, this);
        return Peer.Send(message);
    }

    public virtual RpcMessage CreateMessage(long callId, bool allowPolymorphism)
    {
        var argumentData = Peer.ArgumentSerializer.Serialize(Context.Arguments!, allowPolymorphism);
        var message = new RpcMessage(
            Context.CallTypeId, callId,
            MethodDef.Service.Name, MethodDef.Name,
            argumentData, Context.Headers);
        return message;
    }

    public abstract void SetResult(object? result, RpcInboundContext? context);
    public abstract void SetError(Exception error, RpcInboundContext? context, bool notifyCancelled = false);
    public abstract bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context);

    public void Unregister(bool notifyCancelled = false)
    {
        if (!Peer.OutboundCalls.Unregister(this))
            return; // Already unregistered

        if (notifyCancelled)
            NotifyCancelled();
    }

    public void NotifyCancelled()
    {
        if (Context.CacheInfoCapture is { ResultSource: null })
            return; // The call had never happened, so no need for cancellation notification

        try {
            var systemCallSender = Peer.Hub.InternalServices.SystemCallSender;
            _ = systemCallSender.Cancel(Peer, Id);
        }
        catch {
            // It's totally fine to ignore any error here:
            // peer.Hub might be already disposed at this point,
            // so SystemCallSender might not be available.
            // In any case, peer on the other side is going to
            // be gone as well after that, so every call there
            // will be cancelled anyway.
        }
    }

    // Protected methods

    protected void RegisterCancellationHandler()
    {
        var cancellationToken = Context.CancellationToken;
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;
        if (TimeoutMs > 0) {
            timeoutCts = new CancellationTokenSource(TimeoutMs);
            linkedCts = timeoutCts.Token.LinkWith(cancellationToken);
            cancellationToken = linkedCts.Token;
        }
        var ctr = cancellationToken.Register(static state => {
            var call = (RpcOutboundCall)state!;
            if (call.Context.CancellationToken.IsCancellationRequested)
                call.SetCancelled(call.Context.CancellationToken, null);
            else {
                // timeoutCts is timed out
                var error = Errors.CallTimeout();
                call.SetError(error, null, true);
            }
        }, this, useSynchronizationContext: false);
        _ = ResultTask.ContinueWith(_ => {
                ctr.Dispose();
                linkedCts?.Dispose();
                timeoutCts?.Dispose();
            },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}

public class RpcOutboundCall<TResult> : RpcOutboundCall
{
    protected readonly TaskCompletionSource<TResult> ResultSource;

    public RpcOutboundCall(RpcOutboundContext context)
        : base(context)
    {
        ResultSource = NoWait
            ? (TaskCompletionSource<TResult>)(object)RpcNoWait.TaskSources.Completed
            : new TaskCompletionSource<TResult>();
        ResultTask = ResultSource.Task;
    }

    public override void SetResult(object? result, RpcInboundContext? context)
    {
        if (ResultSource.TrySetResult((TResult)result!)) {
            Unregister();
            if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetResult(context.Message.ArgumentData);
        }
    }

    public override void SetError(Exception error, RpcInboundContext? context, bool notifyCancelled = false)
    {
        if (ResultSource.TrySetException(error)) {
            Unregister(notifyCancelled);
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetException(error);
        }
    }

    public override bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        var isCancelled = ResultSource.TrySetCanceled(cancellationToken);
        if (isCancelled) {
            Unregister(true);
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetCanceled(cancellationToken);
        }
        return isCancelled;
    }
}
