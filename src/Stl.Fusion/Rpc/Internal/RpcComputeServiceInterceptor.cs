using Stl.CommandR.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Rpc.Internal;

public sealed class RpcComputeServiceInterceptor : SelectingInterceptorBase
{
    public new record Options : SelectingInterceptorBase.Options
    {
        public Options()
            => InterceptorTypes = new[] { typeof(RpcComputeMethodInterceptor), typeof(CommandServiceInterceptor) };
    }

    public RpcComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services) { }
}
