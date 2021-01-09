using System;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception;
using Stl.Generators;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaClientInterceptor : ComputeMethodInterceptorBase
    {
        public new class Options : ComputeMethodInterceptorBase.Options
        {
            public Generator<LTag> VersionGenerator { get; set; } = ConcurrentLTagGenerator.Default;
        }

        protected readonly IReplicator Replicator;
        protected readonly Generator<LTag> VersionGenerator;

        public ReplicaClientInterceptor(
            Options? options,
            IServiceProvider services,
            IReplicator replicator,
            ILoggerFactory? loggerFactory = null)
            : base(options ??= new(), services, loggerFactory)
        {
            Replicator = replicator;
            VersionGenerator = options.VersionGenerator;
        }

        protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        {
            var log = LoggerFactory.CreateLogger<ReplicaClientFunction<T>>();
            return new ReplicaClientFunction<T>(method, Replicator, VersionGenerator, log);
        }

        protected override void ValidateTypeInternal(Type type) { }
    }
}
