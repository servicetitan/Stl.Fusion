using Stl.CommandR.Interception;
using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc;

namespace Stl.Fusion.Rpc.Internal;

public class RpcComputeServiceInterceptor : SelectingInterceptorBase
{
    public new record Options : SelectingInterceptorBase.Options
    {
        public Options()
            => InterceptorTypes = new[] { typeof(RpcComputeMethodInterceptor), typeof(CommandServiceInterceptor) };
    }

    protected readonly RpcHub RpcHub;
    protected readonly RpcPeerResolver RpcPeerResolver;

    public RpcComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        RpcHub = services.RpcHub();
        RpcPeerResolver = services.GetRequiredService<RpcPeerResolver>();
    }

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var baseHandler = base.CreateHandler<T>(initialInvocation, methodDef);
        return invocation => {
            var peer = RpcPeerResolver.Invoke(methodDef, invocation.Arguments);
            if (peer == null) {
                // Local call
                var rpcMethodDef = (RpcMethodDef)methodDef;
                var server = RpcHub.Services.GetRequiredService(rpcMethodDef.Service.ServerType);
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            // Remote call
            return baseHandler.Invoke(invocation);
        };
    }

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
    {
        var type = initialInvocation.Proxy.GetType().NonProxyType();
        var serviceDef = RpcHub.ServiceRegistry[type];
        var methodDef = serviceDef.Methods.FirstOrDefault(m => m.Method == method);
        return methodDef;
    }
}
