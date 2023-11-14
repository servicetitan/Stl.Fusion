using Stl.Rpc.Caching;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundCall(RpcOutboundContext context)
    : RpcCall(context.MethodDef!)
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcOutboundContext, RpcOutboundCall>> FactoryCache = new();

    public readonly RpcOutboundContext Context = context;
    public readonly RpcPeer Peer = context.Peer!; // Calls
    public RpcMessage? Message;
    public Task ResultTask { get; protected init; } = null!;
    public TimeSpan ConnectTimeout;
    public TimeSpan Timeout;

    public static RpcOutboundCall New(RpcOutboundContext context)
        => FactoryCache.GetOrAdd((context.CallTypeId, context.MethodDef!.UnwrappedReturnType), static key => {
            var (callTypeId, tResult) = key;
            var type = RpcCallTypeRegistry.Resolve(callTypeId)
                .OutboundCallType
                .MakeGenericType(tResult);
            return (Func<RpcOutboundContext, RpcOutboundCall>)type.GetConstructorDelegate(typeof(RpcOutboundContext))!;
        }).Invoke(context);

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

    public Task RegisterAndSend()
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

    public Task SendNoWait(bool allowPolymorphism, ChannelWriter<RpcMessage>? sender = null)
    {
        var message = CreateMessage(Context.RelatedCallId, allowPolymorphism);
        Peer.CallLog?.Log(Peer.CallLogLevel, "'{PeerRef}': -> {Call} -> #{RelatedId}", Peer.Ref, this, message.RelatedId);
        return Peer.Send(message, sender);
    }

    public Task SendRegistered(bool isFirst = true)
    {
        RpcMessage message;
        try {
            using (Context.Activate())
                message = CreateMessage(Id, MethodDef.AllowArgumentPolymorphism);
            if (Context.CacheInfoCapture is { Key: null } cacheInfoCapture) {
                cacheInfoCapture.Key = new RpcCacheKey(MethodDef.Service.Name, MethodDef.Name, message.ArgumentData);
                if (cacheInfoCapture.CaptureMode == RpcCacheInfoCaptureMode.KeyOnly) {
                    SetResult(null, null);
                    return Task.CompletedTask;
                }
            }
        }
        catch (Exception error) {
            SetError(error, null, isFirst);
            return Task.CompletedTask;
        }
        Peer.CallLog?.Log(Peer.CallLogLevel, "'{PeerRef}': -> {Call} -> #{RelatedId}", Peer.Ref, this, message.RelatedId);
        return Peer.Send(message);
    }

    public virtual RpcMessage CreateMessage(long relatedId, bool allowPolymorphism)
    {
        var argumentData = Peer.ArgumentSerializer.Serialize(Context.Arguments!, allowPolymorphism);
        var message = new RpcMessage(
            Context.CallTypeId, relatedId,
            MethodDef.Service.Name, MethodDef.Name,
            argumentData, Context.Headers);
        return message;
    }

    public abstract void SetResult(object? result, RpcInboundContext? context);
    public abstract void SetError(Exception error, RpcInboundContext? context, bool assumeCancelled = false);
    public abstract bool Cancel(CancellationToken cancellationToken);

    public virtual Task Reconnect(bool isPeerChanged, CancellationToken cancellationToken)
        => SendRegistered(false);

    public void Unregister(bool notifyCancelled = false)
    {
        if (!Peer.OutboundCalls.Unregister(this))
            return; // Already unregistered

        if (notifyCancelled)
            NotifyCancelled();
    }

    // Protected methods

    protected void NotifyCancelled()
    {
        if (Context.CacheInfoCapture is { CaptureMode: RpcCacheInfoCaptureMode.KeyOnly })
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

    protected void RegisterCancellationHandler()
    {
        var cancellationToken = Context.CancellationToken;
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;
        if (Timeout > TimeSpan.Zero) {
            timeoutCts = new CancellationTokenSource(Timeout);
            linkedCts = timeoutCts.Token.LinkWith(cancellationToken);
            cancellationToken = linkedCts.Token;
        }
        var ctr = cancellationToken.Register(static state => {
            var call = (RpcOutboundCall)state!;
            var cancellationToken = call.Context.CancellationToken;
            if (cancellationToken.IsCancellationRequested)
                call.Cancel(cancellationToken);
            else {
                // timeoutCts is timed out
                var error = Errors.CallTimeout(call.Peer);
                call.SetError(error, null);
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

    public async ValueTask<Result<TResult>> GetResult(CancellationToken cancellationToken = default)
    {
        try {
            return await ResultSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (!e.IsCancellationOf(cancellationToken)) {
            return Result.Error<TResult>(e);
        }
    }

    public override void SetResult(object? result, RpcInboundContext? context)
    {
        var typedResult = default(TResult)!;
        try {
            if (result != null)
                typedResult = (TResult)result;
        }
        catch (InvalidCastException) {
            // Intended
        }
        if (ResultSource.TrySetResult(typedResult)) {
            Unregister();
            if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.DataSource?.TrySetResult(context.Message.ArgumentData);
        }
    }

    public override void SetError(Exception error, RpcInboundContext? context, bool assumeCancelled = false)
    {
        var oce = error as OperationCanceledException;
        var cancellationToken = oce?.CancellationToken ?? default;
        var isResultSet = oce != null
            ? ResultSource.TrySetCanceled(cancellationToken)
            : ResultSource.TrySetException(error);
        if (!isResultSet)
            return;

        Unregister(context == null && !assumeCancelled);
        if (Context.CacheInfoCapture is { } cacheInfoCapture) {
            if (oce != null)
                cacheInfoCapture.DataSource?.TrySetCanceled(cancellationToken);
            else
                cacheInfoCapture.DataSource?.TrySetException(error);
        }
    }

    public override bool Cancel(CancellationToken cancellationToken)
    {
        var isCancelled = ResultSource.TrySetCanceled(cancellationToken);
        if (isCancelled) {
            Unregister(true);
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.DataSource?.TrySetCanceled(cancellationToken);
        }
        return isCancelled;
    }
}
