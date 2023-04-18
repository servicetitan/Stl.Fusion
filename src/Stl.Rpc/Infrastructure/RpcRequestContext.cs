using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcRequestContext
{
    private static readonly AsyncLocal<RpcRequestContext?> CurrentLocal = new();

    public static RpcRequestContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcRequestContext();

    public RpcChannel Channel { get; }
    public RpcRequest Request { get; }
    public RpcBoundRequest? BoundRequest { get; set; }
    public CancellationToken CancellationToken { get; }

    public RpcRequestContext(RpcChannel channel, RpcRequest request, CancellationToken cancellationToken)
    {
        Channel = channel;
        Request = request;
        CancellationToken = cancellationToken;
    }

    public ClosedDisposable<RpcRequestContext?> Activate()
    {
        var oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent, static oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }
}
