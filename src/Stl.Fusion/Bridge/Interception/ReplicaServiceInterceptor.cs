using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    protected readonly IReplicator Replicator;
    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly ReplicaCache ReplicaCache;

    public ReplicaServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    {
        Replicator = services.GetRequiredService<IReplicator>();
        VersionGenerator = services.VersionGenerator<LTag>();
        ReplicaCache = services.GetRequiredService<ReplicaCache>();
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new ReplicaMethodFunction<T>(method, Replicator, VersionGenerator, ReplicaCache);

    protected override void ValidateTypeInternal(Type type) { }
}
