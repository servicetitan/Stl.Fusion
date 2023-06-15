using Stl.Fusion.Client.Cache;
using Stl.Fusion.Interception;
using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Interception;

public class ClientComputeServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    protected readonly RpcClientInterceptor RpcClientInterceptor;
    protected readonly ClientComputedCache Cache;

    public ClientComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        RpcClientInterceptor = services.GetRequiredService<RpcClientInterceptor>();
        Cache = services.GetRequiredService<ClientComputedCache>();
    }

    public virtual void Setup(RpcServiceDef serviceDef)
        => RpcClientInterceptor.Setup(serviceDef);

    public override void Intercept(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? RpcClientInterceptor.GetHandler(invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? RpcClientInterceptor.GetHandler(invocation);
        return handler == null
            ? invocation.Intercepted<TResult>()
            : (TResult)handler.Invoke(invocation)!;
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new ClientComputeMethodFunction<T>(method, Hub.LTagVersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
