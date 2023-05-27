using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public interface IRpcOutboundComputeCall
{
    void TryInvalidate(RpcInboundContext context);
}

public class RpcOutboundComputeCall<T> : RpcOutboundCall<T>, IRpcOutboundComputeCall
{
    protected readonly TaskCompletionSource<Unit> WhenInvalidatedSource = new();

    public Task WhenInvalidated => WhenInvalidatedSource.Task;

    public RpcOutboundComputeCall(RpcOutboundContext context)
        : base(context)
        => context.Headers.Add(FusionRpcHeaders.ComputeMethod);

    public void TryInvalidate(RpcInboundContext context)
        => WhenInvalidatedSource.TrySetResult(default);
}
