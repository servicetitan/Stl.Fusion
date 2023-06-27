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
            if (resultVersion == default || (ResultTask.IsCompleted && ResultVersion != resultVersion)) {
                // Not a compute call or a result w/ different version
                if (WhenInvalidatedSource.TrySetResult(default))
                    Unregister(true);
            }
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
            if (notifyCancelled || resultVersion == default || (ResultTask.IsCompleted && ResultVersion != resultVersion)) {
                // Early failure, not a compute call or a result w/ different version
                if (WhenInvalidatedSource.TrySetResult(default))
                    Unregister(true);
            }
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
        lock (Lock) {
            if (!ResultTask.IsCompleted || (resultVersion != default && ResultVersion != resultVersion))
                return; // Invalidation of some earlier call

            if (WhenInvalidatedSource.TrySetResult(default))
                Unregister();
        }
    }

    // Private methods

    private LTag GetResultVersion(RpcInboundContext? context)
    {
        var versionHeader = context?.Message.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        return versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
    }
}
