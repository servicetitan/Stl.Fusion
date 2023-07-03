using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc.Internal;

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
            using (var scope = RpcOutboundContext.Use())
                call = scope.Context.PrepareCall(rpcMethodDef, invocation.Arguments);
            if (call == null) {
                // No call == no peer -> we invoke it locally
                var server = rpcMethodDef.Service.Server;
                return rpcMethodDef.Invoker.Invoke(server, invocation.Arguments);
            }

            Task resultTask;
            if (call.ConnectTimeoutMs > 0 && !call.Peer.ConnectionState.Value.IsConnected())
                resultTask = GetResultTaskWithConnectTimeout<T>(call);
            else {
                _ = call.RegisterAndSend();
                resultTask = call.ResultTask;
            }

            return rpcMethodDef.ReturnsTask
                ? resultTask
                : rpcMethodDef.IsAsyncVoidMethod
                    ? resultTask.ToValueTask()
                    : ((Task<T>)resultTask).ToValueTask();
        };
    }

    private static async Task<T> GetResultTaskWithConnectTimeout<T>(RpcOutboundCall call)
    {
        var cancellationToken = call.Context.CancellationToken;
        try {
            await call.Peer.ConnectionState
                .WhenConnected(cancellationToken)
                .WaitAsync(TimeSpan.FromMilliseconds(call.ConnectTimeoutMs), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException) {
            throw Errors.Disconnected(call.Peer);
        }

        _ = call.RegisterAndSend();
        var typedResultTask = (Task<T>)call.ResultTask;
        return await typedResultTask.ConfigureAwait(false);
    }
}
