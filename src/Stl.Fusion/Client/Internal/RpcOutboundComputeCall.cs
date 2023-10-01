using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcOutboundComputeCall
{
    LTag ResultVersion { get; }
    void SetInvalidated(RpcInboundContext context);
}

public class RpcOutboundComputeCall<TResult>(RpcOutboundContext context)
    : RpcOutboundCall<TResult>(context), IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource
        = TaskCompletionSourceExt.New<Unit>(); // Must not allow synchronous continuations!

    public LTag ResultVersion { get; protected set; }
    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    public LTag GetResultVersion(RpcInboundContext? context)
    {
        var versionHeader = context?.Message.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        return versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
    }

    public override void SetResult(object? result, RpcInboundContext? context)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (Context.CacheInfoCapture is { CaptureMode: RpcCacheInfoCaptureMode.KeyOnly }) {
            base.SetResult(result, context);
            return;
        }

        var resultVersion = GetResultVersion(context);
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            var typedResult = default(TResult)!;
            try {
                if (result != null)
                    typedResult = (TResult)result;
            }
            catch (InvalidCastException) {
                // Intended
            }
            if (ResultSource.TrySetResult(typedResult)) {
                ResultVersion = resultVersion;
                if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                    cacheInfoCapture.ResultSource?.TrySetResult(context.Message.ArgumentData);
            }
        }
    }

    public override void SetError(Exception error, RpcInboundContext? context, bool notifyCancelled)
    {
        var resultVersion = GetResultVersion(context);
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            if (ResultSource.TrySetException(error)) {
                ResultVersion = resultVersion;
                if (Context.CacheInfoCapture is { } cacheInfoCapture) {
                    if (error is OperationCanceledException)
                        cacheInfoCapture.ResultSource?.TrySetCanceled();
                    else
                        cacheInfoCapture.ResultSource?.TrySetResult(default);
                }
            }
            if (notifyCancelled)
                SetInvalidatedUnsafe(true);
        }
    }

    public override bool Cancel(CancellationToken cancellationToken)
    {
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            var isCancelled = ResultSource.TrySetCanceled(cancellationToken);
            if (isCancelled && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetCanceled();
            WhenInvalidatedSource.TrySetResult(default);
            Unregister(true);
            return isCancelled;
        }
    }

    public void SetInvalidated(RpcInboundContext? context)
        // Let's be pessimistic here and ignore version check here
        => SetInvalidated(false);

    public void SetInvalidated(bool notifyCancelled)
    {
        lock (Lock) {
            if (SetInvalidatedUnsafe(notifyCancelled)) {
                if (ResultSource.TrySetCanceled() && Context.CacheInfoCapture is { } cacheInfoCapture)
                    cacheInfoCapture.ResultSource?.TrySetCanceled();
            }
        }
    }

    // Private methods

    private bool SetInvalidatedUnsafe(bool notifyCancelled)
    {
        if (!WhenInvalidatedSource.TrySetResult(default))
            return false;

        Unregister(notifyCancelled);
        return true;
    }

    private bool IsCorrectVersion(LTag resultVersion)
    {
        if (resultVersion == default)
            return false; // Not a compute method call
        if (!ResultTask.IsCompleted)
            return false; // No version is available yet

        // We already have a result w/ mismatching version
        return ResultVersion == resultVersion;
    }
}
