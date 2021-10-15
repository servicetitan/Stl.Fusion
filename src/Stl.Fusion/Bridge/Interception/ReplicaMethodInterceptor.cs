using Stl.Fusion.Interception;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaMethodInterceptor : ComputeMethodInterceptorBase
{
    public new class Options : ComputeMethodInterceptorBase.Options
    {
        public VersionGenerator<LTag>? VersionGenerator { get; set; }
    }

    protected readonly IReplicator Replicator;
    protected readonly VersionGenerator<LTag> VersionGenerator;

    public ReplicaMethodInterceptor(
        Options? options,
        IServiceProvider services,
        IReplicator replicator,
        ILoggerFactory? loggerFactory = null)
        : base(options ??= new(), services, loggerFactory)
    {
        Replicator = replicator;
        VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();
    }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
    {
        var log = LoggerFactory.CreateLogger<ReplicaMethodFunction<T>>();
        return new ReplicaMethodFunction<T>(method, Replicator, VersionGenerator, log);
    }

    protected override void ValidateTypeInternal(Type type) { }
}
