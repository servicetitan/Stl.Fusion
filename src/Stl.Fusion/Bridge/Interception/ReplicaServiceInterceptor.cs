using System;
using Microsoft.Extensions.Logging;
using Stl.CommandR.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaServiceInterceptor : SelectingInterceptorBase
    {
        public new class Options : SelectingInterceptorBase.Options
        {
            public Options() => InterceptorTypes =
                new[] { typeof(ReplicaMethodInterceptor), typeof(CommandServiceInterceptor) };
        }

        public ReplicaServiceInterceptor(Options options, IServiceProvider services, ILoggerFactory? loggerFactory = null)
            : base(options, services, loggerFactory) { }
    }
}
