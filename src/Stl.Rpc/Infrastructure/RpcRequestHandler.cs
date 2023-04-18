namespace Stl.Rpc.Infrastructure;

public class RpcRequestHandler : RpcServiceBase
{
    private static readonly MethodInfo HandleBoundMethod = typeof(RpcRequestHandler)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(HandleBound)));

    private readonly Func<RpcMethodDef, Func<RpcRequestContext, Task>> _createHandleBoundInvoker;
    private readonly ConcurrentDictionary<RpcMethodDef, Func<RpcRequestContext, Task>> _handleBoundInvokers = new();

    private RpcRequestBinder RequestBinder { get; }

    public RpcRequestHandler(IServiceProvider services) : base(services)
    {
        RequestBinder = services.GetRequiredService<RpcRequestBinder>();
        _createHandleBoundInvoker = methodDef => (Func<RpcRequestContext, Task>)HandleBoundMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .CreateDelegate(typeof(Func<RpcRequestContext, Task>));
    }

    public virtual Task Handle(RpcRequestContext context)
    {
        try {
            var boundRequest = RequestBinder.ToBound(context.Request, context.Channel);
            context.BoundRequest = boundRequest;
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to convert RpcRequest to RpcBoundRequest");
            return Task.CompletedTask;
        }

        var handleBoundInvoker = GetHandleBoundInvoker(context.BoundRequest.MethodDef);
        return handleBoundInvoker.Invoke(context);
    }

    // Protected methods

    protected Func<RpcRequestContext, Task> GetHandleBoundInvoker(RpcMethodDef methodDef)
        => _handleBoundInvokers.GetOrAdd(methodDef, _createHandleBoundInvoker);

    protected virtual Task HandleBound<T>(RpcRequestContext context)
    {
        var request = context.BoundRequest!;
        var methodDef = request.MethodDef;
        var arguments = request.Arguments;
        var service = Services.GetRequiredService(methodDef.Service.Type);
        arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        return Task.CompletedTask;
    }
}
