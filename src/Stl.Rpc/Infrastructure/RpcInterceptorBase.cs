using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcInterceptorBase : InterceptorBase
{
    public new record Options : InterceptorBase.Options;

    private RpcHub? _rpcHub;

    public RpcHub RpcHub => _rpcHub ??= Services.RpcHub();
    public RpcServiceDef ServiceDef { get; private set; } = null!;

    protected RpcInterceptorBase(Options options, IServiceProvider services)
        : base(options, services)
    { }

    public void Configure(RpcServiceDef serviceDef)
    {
        if (ServiceDef != null)
            throw Errors.AlreadyInitialized(nameof(ServiceDef));

        ServiceDef = serviceDef;
    }

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
        => ServiceDef.Methods.FirstOrDefault(m => m.Method == method);

    protected override void ValidateTypeInternal(Type type)
    { }
}
