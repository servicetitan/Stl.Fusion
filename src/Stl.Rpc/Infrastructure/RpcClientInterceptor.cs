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
                call = scope.Context.PrepareCall(rpcMethodDef, invocation.Arguments);
                if (call != null)
                    sendTask = call.RegisterAndSend();
            }
            if (call == null) {
                // No call == no peer -> we invoke it locally
                var server = rpcMethodDef.Service.Server;
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            if (!call.NoWait) {
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
            if (!call1.SetCancelled(call1.Context.CancellationToken, null))
                return;

            // If we're here, we know the outgoing call is successfully cancelled.
            // We notify peer about that only in this case.
            var peer = call1.Peer;
            var systemCallSender = peer.Hub.SystemCallSender;
            _ = systemCallSender.Cancel(peer, call1.Id);
        }, call, false);
        _ = call.ResultTask.ContinueWith(
            _ => ctr.Dispose(),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
