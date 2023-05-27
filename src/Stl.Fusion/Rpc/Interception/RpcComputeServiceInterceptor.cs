using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Rpc.Interception;

public class RpcComputeServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    private VersionGenerator<LTag>? _versionGenerator;
    private RpcComputedCache? _cache;

    protected VersionGenerator<LTag> VersionGenerator => _versionGenerator ??= Services.VersionGenerator<LTag>();
    protected RpcComputedCache Cache => _cache ??= Services.GetRequiredService<RpcComputedCache>();

    public RpcComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    { }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new RpcComputeMethodFunction<T>(method, VersionGenerator, Cache, Services);

    protected override void ValidateTypeInternal(Type type) { }
}
