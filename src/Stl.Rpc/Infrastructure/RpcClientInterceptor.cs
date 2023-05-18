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

    public RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => Hub = services.GetRequiredService<RpcHub>();

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            using var scope = RpcOutboundContext.NewOrActive();
            var context = scope.Context;
            _ = context.StartCall(rpcMethodDef, invocation.Arguments);
            var call = context.Call!;
            var resultTask = call.ResultTask;
#pragma warning disable CA2012
            return rpcMethodDef.ReturnsTask
                ? resultTask
                : rpcMethodDef.IsAsyncVoidMethod
                    ? resultTask.ToValueTask()
                    : ((Task<T>)resultTask).ToValueTask();
#pragma warning restore CA2012
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
