namespace Stl.Rpc.Infrastructure;

public class RpcRequestHandler : RpcMiddleware
{
    private RpcRequestBinder RequestBinder { get; }

    public RpcRequestHandler(IServiceProvider services) : base(services) 
        => RequestBinder = services.GetRequiredService<RpcRequestBinder>();

    public override Task Invoke(RpcRequestContext context, CancellationToken cancellationToken)
    {
        try {
            var boundRequest = RequestBinder.ToBound(context.Request, context.Channel);
            context.BoundRequest = boundRequest;
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to convert RpcRequest to RpcBoundRequest");
            return Task.CompletedTask;
        }

        return context.InvokeNextMiddleware(cancellationToken);
    }
}
