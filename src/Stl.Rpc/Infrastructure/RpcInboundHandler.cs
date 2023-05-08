namespace Stl.Rpc.Infrastructure;

public class RpcInboundHandler : RpcServiceBase
{
    private static readonly MethodInfo InvokeMethod = typeof(RpcInboundHandler)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(Invoke)));

    private readonly Func<RpcMethodDef, Func<RpcInboundContext, Task>> _createInvoker;
    private readonly ConcurrentDictionary<RpcMethodDef, Func<RpcInboundContext, Task>> _invokers = new();

    private RpcCallConverter CallConverter { get; }

    public RpcInboundHandler(IServiceProvider services) : base(services)
    {
        CallConverter = services.GetRequiredService<RpcCallConverter>();
        _createInvoker = methodDef => (Func<RpcInboundContext, Task>)InvokeMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .CreateDelegate(typeof(Func<RpcInboundContext, Task>));
    }

    public virtual Task Handle(RpcInboundContext context)
    {
        var call = context.Call = CallConverter.ToCall(context.Peer, context.Message);
        var invoker = GetInvoker(call.MethodDef);
        return invoker.Invoke(context);
    }

    // Protected methods

    protected Func<RpcInboundContext, Task> GetInvoker(RpcMethodDef methodDef)
        => _invokers.GetOrAdd(methodDef, _createInvoker);

    protected virtual Task Invoke<T>(RpcInboundContext context)
    {
        var call = context.Call!;
        var methodDef = call.MethodDef;
        var arguments = call.Arguments;
        var service = Services.GetRequiredService(methodDef.Service.Type);
        arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        return Task.CompletedTask;
    }
}
