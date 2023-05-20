using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor : InterceptorBase
{
    private RpcServiceRegistry? _serviceRegistry;

    public new record Options : InterceptorBase.Options
    { }

    protected RpcHub Hub { get; }
    protected RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Hub.ServiceRegistry;

    public RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => Hub = services.GetRequiredService<RpcHub>();

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            using var scope = RpcOutboundContext.NewOrActive();
            var context = scope.Context;
            var sendTask = context.SendCall(rpcMethodDef, invocation.Arguments);
            if (context.Peer == null) {
                // No peer -> we invoke it locally
                var server = rpcMethodDef.Hub.Services.GetRequiredService(rpcMethodDef.Service.ServerType);
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            if (!rpcMethodDef.NoWait) {
                _ = sendTask.ContinueWith(t => {
                    if (!t.IsCompletedSuccessfully()) {
                        // Send failed -> complete with an error
                        context.Call!.TryCompleteWithError(t.ToResultSynchronously().Error!);
                        return;
                    }

                    // Send succeeded -> wire up cancellation
                    var ctr = context.CancellationToken.Register(static state => {
                        var context1 = (RpcOutboundContext)state!;
                        if (!context1.Call!.TryCompleteWithCancel(context1.CancellationToken))
                            return;

                        var peer1 = context1.Peer!;
                        var call1 = context1.Call!;
                        var systemCallSender = peer1.Hub.SystemCallSender;
                        _ = systemCallSender.Cancel(peer1, call1.Id);
                    }, context, false);
                    _ = context.Call!.ResultTask.ContinueWith(
                        _ => ctr.Dispose(),
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            var call = context.Call!;
            var resultTask = call.ResultTask;
#pragma warning disable CA2012
            return rpcMethodDef.ReturnsTask
                ? resultTask
                : rpcMethodDef.IsAsyncVoidMethod
                    ? resultTask.ToValueTask()
                    : ((Task<T>)resultTask).ToValueTask();
#pragma warning restore CA2012
        };
    }

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
    {
        var type = initialInvocation.Proxy.GetType().NonProxyType();
        var serviceDef = ServiceRegistry[type];
        var methodDef = serviceDef.Methods.FirstOrDefault(m => m.Method == method);
        return methodDef;
    }

    protected override void ValidateTypeInternal(Type type)
    { }
}
