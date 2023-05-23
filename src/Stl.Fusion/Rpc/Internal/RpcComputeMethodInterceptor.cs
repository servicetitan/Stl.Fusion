using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Rpc.Internal;

public class RpcComputeMethodInterceptor : ComputeMethodInterceptorBase
{
    public new record Options : ComputeMethodInterceptorBase.Options
    { }

    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly RpcComputedCache Cache;

    public RpcComputeMethodInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        VersionGenerator = services.VersionGenerator<LTag>();
        Cache = services.GetRequiredService<RpcComputedCache>();
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new RpcComputeMethodFunction<T>(method, VersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
