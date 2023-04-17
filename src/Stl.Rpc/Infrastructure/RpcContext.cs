using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcContext
{
    public RpcChannel Channel { get; }
    public RpcRequest Request { get; }
    public ParsedRpcRequest? ParsedRequest { get; set; }
    public RpcRequestProcessingState ProcessingState { get; private set; }

    public RpcContext(RpcChannel channel, RpcRequest request)
    {
        Channel = channel;
        Request = request;
        ProcessingState = new RpcRequestProcessingState(channel.Middlewares);
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
