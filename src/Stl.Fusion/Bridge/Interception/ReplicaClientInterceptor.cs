using System;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaClientInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public Generator<LTag> LTagGenerator { get; set; } = ConcurrentLTagGenerator.Default;
        }

        protected Generator<LTag> LTagGenerator { get; }

        public ReplicaClientInterceptor(
            Options options,
            IComputedRegistry? registry = null,
            ILoggerFactory? loggerFactory = null)
            : base(options, registry, loggerFactory)
        {
            RequiresAttribute = false;
            LTagGenerator = options.LTagGenerator;
        }

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method)
        {
            var log = LoggerFactory.CreateLogger<ReplicaClientFunction<T>>();
            return new ReplicaClientFunction<T>(method, LTagGenerator, Registry, log);
        }

        protected override void ValidateTypeInternal(Type type)
        {
            if (!typeof(IReplicaClient).IsAssignableFrom(type))
                throw Errors.MustImplement<IReplicaClient>(type);
        }
    }
}
