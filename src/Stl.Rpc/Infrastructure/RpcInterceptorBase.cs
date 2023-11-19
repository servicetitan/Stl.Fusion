using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcInterceptorBase(
    RpcInterceptorBase.Options settings,
    IServiceProvider services
    ) : InterceptorBase(settings, services)
{
    public new record Options : InterceptorBase.Options;

    private RpcHub? _rpcHub;

    public RpcHub Hub => _rpcHub ??= Services.RpcHub();
    public RpcServiceDef ServiceDef { get; private set; } = null!;

    public void Setup(RpcServiceDef serviceDef)
    {
        if (ServiceDef != null)
            throw Errors.AlreadyInitialized(nameof(ServiceDef));

        ServiceDef = serviceDef;
    }

    // We don't need to decorate this method with any dynamic access attributes
    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
        => ServiceDef.Methods.FirstOrDefault(m => m.Method == method);

    protected override void ValidateTypeInternal(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    { }
}
