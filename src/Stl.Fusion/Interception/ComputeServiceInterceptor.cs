using Stl.CommandR.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public sealed class ComputeServiceInterceptor : SelectingInterceptorBase
{
    public new record Options : SelectingInterceptorBase.Options
    {
        public Options()
            => InterceptorTypes = new[] { typeof(ComputeMethodInterceptor), typeof(CommandServiceInterceptor) };
    }

    public ComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services) { }
}
