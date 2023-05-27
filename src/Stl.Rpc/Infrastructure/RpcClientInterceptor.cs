using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor : InterceptorBase
{
    private RpcServiceDef? _serviceDef;

    public new record Options : InterceptorBase.Options;

    public RpcHub Hub { get; }
    public RpcServiceDef ServiceDef {
        get => _serviceDef ?? throw Errors.NotInitialized(nameof(ServiceDef));
        set {
            if (_serviceDef != null)
                throw Errors.AlreadyInitialized(nameof(ServiceDef));

            _serviceDef = value;
        }
    }

    public RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => Hub = services.GetRequiredService<RpcHub>();

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            RpcOutboundCall? call;
            Task sendTask;
            using (var scope = RpcOutboundContext.Use()) {
                call = scope.Context.Bind(rpcMethodDef, invocation.Arguments);
                sendTask = call?.Send().AsTask() ?? Task.CompletedTask;
            }
            if (call == null) {
                // No call == no peer -> we invoke it locally
                var server = rpcMethodDef.Hub.Services.GetRequiredService(rpcMethodDef.Service.ServerType);
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            if (!rpcMethodDef.NoWait) {
                _ = sendTask.ContinueWith(t => {
                    if (!t.IsCompletedSuccessfully()) {
                        // Send failed -> complete with an error
                        call.TryCompleteWithError(t.ToResultSynchronously().Error!, null);
                        return;
                    }

                    // Send succeeded -> wire up cancellation
                    var ctr = call.Context.CancellationToken.Register(static state => {
                        var call1 = (RpcOutboundCall)state!;
                        var context1 = call1.Context;
                        if (!call1.TryCompleteWithCancel(context1.CancellationToken, null))
                            return;

                        // If we're here, we know the outgoing call is successfully cancelled.
                        // We notify peer about that only in this case. 
                        var peer1 = context1.Peer!;
                        var systemCallSender = peer1.Hub.SystemCallSender;
                        _ = systemCallSender.Cancel(peer1, call1.Id);
                    }, call, false);
                    _ = call.ResultTask.ContinueWith(
                        _ => ctr.Dispose(),
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

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
        => ServiceDef.Methods.FirstOrDefault(m => m.Method == method);

    protected override void ValidateTypeInternal(Type type)
    { }
}
