using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcRoutingInterceptor : RpcInterceptorBase
{
    public new record Options : RpcInterceptorBase.Options;

    public object LocalService { get; private set; }
    public object RemoteService { get; private set; }

    protected readonly RpcPeerResolver RpcPeerResolver;

    public RpcRoutingInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => RpcPeerResolver = services.GetRequiredService<RpcPeerResolver>();

    public void Configure(RpcServiceDef serviceDef, object localService, object remoteService)
    {
        base.Configure(serviceDef);
        LocalService = localService;
        RemoteService = remoteService;
    }


    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
        => invocation => {
            var peer = RpcPeerResolver.Invoke(methodDef, invocation.Arguments);
            var service = peer == null ? LocalService : RemoteService;
            var rpcMethodDef = (RpcMethodDef)methodDef;
            return rpcMethodDef.Invoker.Invoke(service, invocation.Arguments);
        };

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
        => ServiceDef.Methods.FirstOrDefault(m => m.Method == method);
}
