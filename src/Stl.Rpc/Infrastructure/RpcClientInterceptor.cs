using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor : RpcInterceptorBase
{
    public new record Options : RpcInterceptorBase.Options
    {
        public static Options Default { get; set; } = new();
    }

    public RpcClientInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    { }

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var rpcMethodDef = (RpcMethodDef)methodDef;
        return invocation => {
            RpcOutboundCall? call;
            ValueTask sendTask = default;
            using (var scope = RpcOutboundContext.Use()) {
                call = scope.Context.SetCall(rpcMethodDef, invocation.Arguments);
                if (call != null)
                    sendTask = call.Send();
            }
            if (call == null) {
                // No call == no peer -> we invoke it locally
                var server = rpcMethodDef.Service.Server;
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            if (!rpcMethodDef.NoWait) {
                if (sendTask.IsCompletedSuccessfully)
                    CompleteSend(call);
                else
                    _ = CompleteSend(sendTask, call);
            }

            var resultTask = call.ResultTask;
            return rpcMethodDef.ReturnsTask
                ? resultTask
                : rpcMethodDef.IsAsyncVoidMethod
                    ? resultTask.ToValueTask()
                    : ((Task<T>)resultTask).ToValueTask();
        };
    }

    private static async Task CompleteSend(ValueTask sendTask, RpcOutboundCall call)
    {
        try {
            await sendTask.ConfigureAwait(false);
        }
        catch (Exception error) {
            // Should never happen, but
            call.SetError(error, null);
            return;
        }
        CompleteSend(call);
    }

    private static void CompleteSend(RpcOutboundCall call)
    {
        // Send succeeded -> wire up cancellation
        var ctr = call.Context.CancellationToken.Register(static state => {
            var call1 = (RpcOutboundCall)state!;
            var context1 = call1.Context;
            if (!call1.SetCancelled(context1.CancellationToken, null))
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
    }
}
