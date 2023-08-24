using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcClientInterceptor(
    RpcClientInterceptor.Options settings,
    IServiceProvider services
    ) : RpcInterceptorBase(settings, services)
{
    public new record Options : RpcInterceptorBase.Options
    {
        public static Options Default { get; set; } = new();
    }

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
            if (call.ConnectTimeout > TimeSpan.Zero && !call.Peer.ConnectionState.Value.IsConnected())
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
        using var cts = new CancellationTokenSource(call.ConnectTimeout);
        using var linkedCts = cancellationToken.LinkWith(cts.Token);
        try {
            await call.Peer.ConnectionState.WhenConnected(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) {
            throw Errors.Disconnected(call.Peer);
        }

        _ = call.RegisterAndSend();
        var typedResultTask = (Task<T>)call.ResultTask;
        return await typedResultTask.ConfigureAwait(false);
    }
}
