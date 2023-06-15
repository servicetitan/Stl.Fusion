using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcOutboundComputeCall
{
    LTag ResultVersion { get; }
    void TryInvalidate(RpcInboundContext context);
}

public class RpcOutboundComputeCall<TResult> : RpcOutboundCall<TResult>, IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource = new();

    public LTag ResultVersion { get; protected set; } = default;
    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    public RpcOutboundComputeCall(RpcOutboundContext context)
        : base(context)
        => context.Headers.Add(RpcSystemHeaders.CallType.With(RpcComputeCall.CallTypeId));

    public override bool TryCompleteWithOk(object? result, RpcInboundContext context)
    {
        try {
            return ResultSource.TrySetResult((TResult)result!) && TryComplete(context);
        }
        catch (Exception e) {
            return TryCompleteWithError(e, context);
        }
    }

    public override bool TryCompleteWithError(Exception error, RpcInboundContext? context)
        => ResultSource.TrySetException(error) && TryComplete(context);

    public void TryInvalidate(RpcInboundContext context)
        => WhenInvalidatedSource.TrySetResult(default);

    private bool TryComplete(RpcInboundContext? context)
    {
        var versionHeader = context?.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        ResultVersion = versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
        if (ResultVersion == default)
            Context.Peer!.Calls.Outbound.TryRemove(Id, this);
        return true;
    }
}
