using System;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaClientInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public Generator<LTag> VersionGenerator { get; set; } = ConcurrentLTagGenerator.Default;
        }

        protected readonly IReplicator Replicator;
        protected readonly Generator<LTag> VersionGenerator;

        public ReplicaClientInterceptor(
            Options options,
            IReplicator replicator,
            IComputedRegistry? registry = null,
            ILoggerFactory? loggerFactory = null)
            : base(options, registry, loggerFactory)
        {
            RequiresAttribute = false;
            Replicator = replicator;
            VersionGenerator = options.VersionGenerator;
        }

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method)
        {
            var log = LoggerFactory.CreateLogger<ReplicaClientFunction<T>>();
            return new ReplicaClientFunction<T>(method, Replicator, VersionGenerator, Registry, log);
        }

        protected override void ValidateTypeInternal(Type type) { }
    }
}
