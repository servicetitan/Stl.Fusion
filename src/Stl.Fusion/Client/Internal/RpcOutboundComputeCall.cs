using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcOutboundComputeCall
{
    LTag ResultVersion { get; }
    void SetInvalidated(RpcInboundContext context);
}

public class RpcOutboundComputeCall<TResult> : RpcOutboundCall<TResult>, IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource = TaskCompletionSourceExt.New<Unit>();

    public LTag ResultVersion { get; protected set; } = default;
    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    public RpcOutboundComputeCall(RpcOutboundContext context) : base(context)
    { }

    public override void SetResult(object? result, RpcInboundContext? context)
    {
        var resultVersion = GetResultVersion(context);
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            if (IsResultVersionChanged(resultVersion))
                SetInvalidated(true);

            if (ResultSource.TrySetResult((TResult)result!)) {
                ResultVersion = resultVersion;
                if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                    cacheInfoCapture.ResultSource?.TrySetResult(context.Message.ArgumentData);
            }
        }
    }

    public override void SetError(Exception error, RpcInboundContext? context, bool notifyCancelled = false)
    {
        var resultVersion = GetResultVersion(context);
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            if (notifyCancelled || IsResultVersionChanged(resultVersion))
                SetInvalidated(true);

            if (ResultSource.TrySetException(error)) {
                ResultVersion = resultVersion;
                if (Context.CacheInfoCapture is { } cacheInfoCapture)
                    cacheInfoCapture.ResultSource?.TrySetException(error);
            }
        }
    }

    public override bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        // We always use Lock to update ResultSource in this type
        lock (Lock) {
            // No need to call WhenInvalidatedSource.TrySetResult() here,
            // coz ClientComputeMethodFunction won't get to a point
            // where it's going to await for invalidation.
            return base.SetCancelled(cancellationToken, context);
        }
    }

    public void SetInvalidated(RpcInboundContext context)
    {
        var resultVersion = GetResultVersion(context);
        if (resultVersion == default)
            return; // Should never happen

        lock (Lock) {
            if (!ResultTask.IsCompleted || ResultVersion != resultVersion)
                return; // No result yet or version mismatch -> nothing to invalidate

            SetInvalidated();
        }
    }

    // Private methods

    private void SetInvalidated(bool notifyCancelled = false)
    {
        if (WhenInvalidatedSource.TrySetResult(default))
            Unregister(notifyCancelled);
    }

    private bool IsResultVersionChanged(LTag resultVersion)
    {
        if (resultVersion == default)
            return true; // Not a compute method call

        // We already have a result w/ mismatching version
        return ResultTask.IsCompleted && ResultVersion != resultVersion;
    }

    private LTag GetResultVersion(RpcInboundContext? context)
    {
        var versionHeader = context?.Message.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        return versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
    }
}
