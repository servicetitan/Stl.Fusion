namespace Stl.Rpc.Infrastructure;

public class RpcRequestHandler : RpcServiceBase
{
    private static readonly MethodInfo InvokeMethod = typeof(RpcRequestHandler)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(Invoke)));

    private readonly Func<RpcMethodDef, Func<RpcRequestContext, Task>> _createInvoker;
    private readonly ConcurrentDictionary<RpcMethodDef, Func<RpcRequestContext, Task>> _invokers = new();

    private RpcRequestBinder RequestBinder { get; }

    public RpcRequestHandler(IServiceProvider services) : base(services)
    {
        RequestBinder = services.GetRequiredService<RpcRequestBinder>();
        _createInvoker = methodDef => (Func<RpcRequestContext, Task>)InvokeMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .CreateDelegate(typeof(Func<RpcRequestContext, Task>));
    }

    public virtual Task Handle(RpcRequestContext context)
    {
        try {
            var boundRequest = RequestBinder.ToBound(context.Peer, context.Message);
            context.BoundRequest = boundRequest;
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to convert RpcRequest to RpcBoundRequest");
            return Task.CompletedTask;
        }

        var invoker = GetInvoker(context.BoundRequest.MethodDef);
        return invoker.Invoke(context);
    }

    // Protected methods

    protected Func<RpcRequestContext, Task> GetInvoker(RpcMethodDef methodDef)
        => _invokers.GetOrAdd(methodDef, _createInvoker);

    protected virtual Task Invoke<T>(RpcRequestContext context)
    {
        var request = context.BoundRequest!;
        var methodDef = request.MethodDef;
        var arguments = request.Arguments;
        var service = Services.GetRequiredService(methodDef.Service.Type);
        arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        return Task.CompletedTask;
    }
}
