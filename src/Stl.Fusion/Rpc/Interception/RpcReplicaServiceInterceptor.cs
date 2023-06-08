using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Rpc.Interception;

public class RpcReplicaServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    protected VersionGenerator<LTag> VersionGenerator;
    protected RpcComputedCache Cache;

    public RpcReplicaServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        VersionGenerator = services.VersionGenerator<LTag>();
        Cache = services.GetRequiredService<RpcComputedCache>();
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new RpcReplicaMethodFunction<T>(method, VersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
