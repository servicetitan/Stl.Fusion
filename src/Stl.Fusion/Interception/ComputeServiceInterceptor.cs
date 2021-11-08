using Stl.CommandR.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public class ComputeServiceInterceptor : SelectingInterceptorBase
{
    public new class Options : SelectingInterceptorBase.Options
    {
        public Options() => InterceptorTypes =
            new[] { typeof(ComputeMethodInterceptor), typeof(CommandServiceInterceptor) };
    }

    public ComputeServiceInterceptor(Options options, IServiceProvider services, ILoggerFactory? loggerFactory = null)
        : base(options, services, loggerFactory) { }
}
