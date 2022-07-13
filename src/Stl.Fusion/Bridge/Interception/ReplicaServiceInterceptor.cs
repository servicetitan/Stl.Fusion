using Stl.CommandR.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaServiceInterceptor : SelectingInterceptorBase
{
    public new record Options : SelectingInterceptorBase.Options
    {
        public Options()
            => InterceptorTypes = new[] { typeof(ReplicaMethodInterceptor), typeof(CommandServiceInterceptor) };
    }

    public ReplicaServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services) { }
}
