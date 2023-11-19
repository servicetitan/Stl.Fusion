using System.Diagnostics.CodeAnalysis;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcOutboundComputeCall
{
    LTag ResultVersion { get; }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    void SetInvalidated(RpcInboundContext context);
}

public class RpcOutboundComputeCall<TResult>(RpcOutboundContext context)
    : RpcOutboundCall<TResult>(context), IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource
        = TaskCompletionSourceExt.New<Unit>(); // Must not allow synchronous continuations!

    public LTag ResultVersion { get; protected set; }
    // ReSharper disable once InconsistentlySynchronizedField
    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public override Task Reconnect(bool isPeerChanged, CancellationToken cancellationToken)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (!isPeerChanged || !ResultSource.Task.IsCompleted)
            return base.Reconnect(isPeerChanged, cancellationToken);

        SetInvalidated(false);
        return Task.CompletedTask;
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public override void SetResult(object? result, RpcInboundContext? context)
    {
        var resultVersion = context.GetResultVersion();
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            // The code below is a copy of base.SetResult
            // except the Unregister call in the end.
            // We don't unregister the call here, coz
            // we'll need to await for invalidation
            var typedResult = default(TResult)!;
            try {
                if (result != null)
                    typedResult = (TResult)result;
            }
            catch (InvalidCastException) {
                // Intended
            }

            if (!ResultSource.TrySetResult(typedResult)) {
                // Result is already set
                if (context == null || ResultVersion != resultVersion)  // Non-peer set or version mismatch
                    SetInvalidatedUnsafe(true);
                return;
            }

            ResultVersion = resultVersion;
            if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.DataSource?.TrySetResult(context.Message.ArgumentData);
        }
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public override void SetError(Exception error, RpcInboundContext? context, bool assumeCancelled = false)
    {
        var resultVersion = context.GetResultVersion();
        var oce = error as OperationCanceledException;
        var cancellationToken = oce?.CancellationToken ?? default;
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            var isResultSet = oce != null
                ? ResultSource.TrySetCanceled(cancellationToken)
                : ResultSource.TrySetException(error);
            if (!isResultSet) {
                // Result was set earlier
                if (context == null || ResultVersion != resultVersion)  // Non-peer set or version mismatch
                    SetInvalidatedUnsafe(!assumeCancelled);
                return;
            }

            // Result was just set
            ResultVersion = resultVersion;
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                if (oce != null)
                    cacheInfoCapture.DataSource?.TrySetCanceled(cancellationToken);
                else
                    cacheInfoCapture.DataSource?.TrySetException(error);
            if (context == null) // Non-peer set
                SetInvalidatedUnsafe(!assumeCancelled);
        }
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public override bool Cancel(CancellationToken cancellationToken)
    {
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            var isCancelled = ResultSource.TrySetCanceled(cancellationToken);
            if (isCancelled && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.DataSource?.TrySetCanceled(cancellationToken);
            WhenInvalidatedSource.TrySetResult(default);
            Unregister(true);
            return isCancelled;
        }
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public void SetInvalidated(RpcInboundContext? context)
        // Let's be pessimistic here and ignore version check here
        => SetInvalidated(false);

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    public void SetInvalidated(bool notifyCancelled)
    {
        lock (Lock) {
            if (SetInvalidatedUnsafe(notifyCancelled)) {
                if (ResultSource.TrySetCanceled() && Context.CacheInfoCapture is { } cacheInfoCapture)
                    cacheInfoCapture.DataSource?.TrySetCanceled();
            }
        }
    }

    // Private methods

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    private bool SetInvalidatedUnsafe(bool notifyCancelled)
    {
        if (!WhenInvalidatedSource.TrySetResult(default))
            return false;

        Unregister(notifyCancelled);
        return true;
    }
}
