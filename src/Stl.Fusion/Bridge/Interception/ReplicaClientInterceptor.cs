using System;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;
using Stl.Time;

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
            IMomentClock? clock = null,
            ILoggerFactory? loggerFactory = null)
            : base(options, clock, loggerFactory)
        {
            Replicator = replicator;
            VersionGenerator = options.VersionGenerator;
        }

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method)
        {
            var log = LoggerFactory.CreateLogger<ReplicaClientFunction<T>>();
            return new ReplicaClientFunction<T>(method, Replicator, VersionGenerator, log);
        }

        protected override void ValidateTypeInternal(Type type) { }
    }
}
