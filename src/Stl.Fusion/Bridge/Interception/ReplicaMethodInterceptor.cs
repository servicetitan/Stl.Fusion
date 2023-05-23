using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaMethodInterceptor : ComputeMethodInterceptorBase
{
    public new record Options : ComputeMethodInterceptorBase.Options;

    protected readonly IReplicator Replicator;
    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly ReplicaCache ReplicaCache;

    public ReplicaMethodInterceptor(Options options, IServiceProvider services)
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
