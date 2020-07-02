using System;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Concurrency;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaServiceInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public ConcurrentIdGenerator<LTag> LTagGenerator { get; set; } = ConcurrentIdGenerator.DefaultLTag; 
        }

        protected ConcurrentIdGenerator<LTag> LTagGenerator { get; }

        public ReplicaServiceInterceptor(
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
            var log = LoggerFactory.CreateLogger<ReplicaServiceFunction<T>>();
            return new ReplicaServiceFunction<T>(method, LTagGenerator, Registry, log);
        }

        protected override void ValidateTypeInternal(Type type)
        {
            if (!typeof(IReplicaService).IsAssignableFrom(type))
                throw Errors.MustImplement<IComputedService>(type);
        }
    }
}
