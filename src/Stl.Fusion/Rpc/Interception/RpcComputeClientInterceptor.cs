using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Cache;
using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Versioning;

namespace Stl.Fusion.Rpc.Interception;

public class RpcComputeClientInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    protected readonly RpcClientInterceptor ClientInterceptor;
    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly RpcComputedCache Cache;

    public RpcComputeClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        ClientInterceptor = services.GetRequiredService<RpcClientInterceptor>();
        VersionGenerator = services.VersionGenerator<LTag>();
        Cache = services.GetRequiredService<RpcComputedCache>();
    }

    public override void Intercept(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? ClientInterceptor.GetHandler(invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? ClientInterceptor.GetHandler(invocation);
        return handler == null
            ? invocation.Intercepted<TResult>()
            : (TResult)handler.Invoke(invocation)!;
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new RpcComputeMethodFunction<T>(method, VersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
