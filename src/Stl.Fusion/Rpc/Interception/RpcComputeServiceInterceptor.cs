using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Cache;
using Stl.Versioning;

namespace Stl.Fusion.Rpc.Interception;

public class RpcComputeServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly RpcComputedCache Cache;

    public RpcComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        VersionGenerator = services.VersionGenerator<LTag>();
        Cache = services.GetRequiredService<RpcComputedCache>();
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new RpcComputeMethodFunction<T>(method, VersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
