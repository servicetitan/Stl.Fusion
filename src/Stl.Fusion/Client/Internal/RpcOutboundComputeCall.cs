using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcOutboundComputeCall
{
    LTag ResultVersion { get; }
    void SetInvalidated(RpcInboundContext context);
}

public class RpcOutboundComputeCall<TResult> : RpcOutboundCall<TResult>, IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource = new();

    public LTag ResultVersion { get; protected set; } = default;
    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    public RpcOutboundComputeCall(RpcOutboundContext context) : base(context)
    { }

    public override void SetResult(object? result, RpcInboundContext context)
    {
        if (ResultSource.TrySetResult((TResult)result!))
            UnregisterIfVersionIsMissing(context);
    }

    public override void SetError(Exception error, RpcInboundContext? context)
    {
        if (ResultSource.TrySetException(error))
            UnregisterIfVersionIsMissing(context);
    }

    public void SetInvalidated(RpcInboundContext context)
    {
        if (WhenInvalidatedSource.TrySetResult(default)) {
            ResultSource.TrySetResult(default!);
            Unregister();
        }
    }

    // Private methods

    private void UnregisterIfVersionIsMissing(RpcInboundContext? context)
    {
        var versionHeader = context?.Message.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        ResultVersion = versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
        if (ResultVersion == default)
            Unregister();
    }
}
