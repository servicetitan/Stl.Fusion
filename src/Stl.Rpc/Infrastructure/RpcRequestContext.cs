using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcRequestContext
{
    private static readonly AsyncLocal<RpcRequestContext?> CurrentLocal = new();

    public static RpcRequestContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcRequestContext();

    public RpcChannel Channel { get; }
    public RpcRequest Request { get; }
    public RpcBoundRequest? BoundRequest { get; set; }
    public RpcRequestProcessingState ProcessingState { get; private set; }

    public RpcRequestContext(RpcChannel channel, RpcRequest request)
    {
        Channel = channel;
        Request = request;
        ProcessingState = new RpcRequestProcessingState(channel.Middlewares);
    }

    public ClosedDisposable<RpcRequestContext?> Activate()
    {
        var oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent, static oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }

    public Task InvokeNextMiddleware(CancellationToken cancellationToken)
    {
        if (ProcessingState.IsFinal)
            throw Errors.NoMoreMiddlewares();

        var middleware = ProcessingState.NextMiddleware;
        ProcessingState = ProcessingState.NextState;
        return middleware.Invoke(this, cancellationToken);
    }
}
