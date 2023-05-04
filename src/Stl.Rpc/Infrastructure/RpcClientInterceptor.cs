using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor : InterceptorBase
{
    public new record Options : InterceptorBase.Options
    { }

    protected RpcServiceRegistry ServiceRegistry { get; }

    protected RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();
    }

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            var boundRequest = new RpcBoundRequest<T>(rpcMethodDef, invocation.Arguments);
            // TODO: Find RpcChannel & push request there
            return rpcMethodDef.ReturnsValueTask
                ? rpcMethodDef.IsAsyncVoidMethod
                    ? boundRequest.UntypedResultTask.ToValueTask()
                    : boundRequest.ResultTask.ToValueTask()
                : boundRequest.ResultTask;
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
