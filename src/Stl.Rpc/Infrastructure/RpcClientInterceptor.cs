using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor : InterceptorBase
{
    private RpcServiceRegistry? _serviceRegistry;

    public new record Options : InterceptorBase.Options
    { }

    protected RpcHub Hub { get; }
    protected RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Hub.ServiceRegistry;

    protected RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => Hub = services.GetRequiredService<RpcHub>();

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            var call = new RpcCall<T>(rpcMethodDef, invocation.Arguments);
            // TODO: Find RpcChannel & push request there
            return rpcMethodDef.ReturnsValueTask
                ? rpcMethodDef.IsAsyncVoidMethod
                    ? call.UntypedResultTask.ToValueTask()
                    : call.ResultTask.ToValueTask()
                : call.ResultTask;
        };
    }

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
    {
        var type = initialInvocation.Proxy.GetType().NonProxyType();
        var serviceDef = ServiceRegistry[type];
        var methodDef = serviceDef.Methods.FirstOrDefault(m => m.Method == method);
        return methodDef;
    }

    protected override void ValidateTypeInternal(Type type)
    { }
}
