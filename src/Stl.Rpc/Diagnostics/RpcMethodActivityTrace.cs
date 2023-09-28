using System.Diagnostics;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Diagnostics;

public class RpcMethodActivityTrace(RpcMethodActivityTracer tracer, Activity activity) : RpcMethodTrace
{
    public override void OnResultTaskReady(RpcInboundCall call)
        => _ = call.UntypedResultTask.ContinueWith(Complete,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    protected void Complete(Task resultTask)
    {
        activity.Dispose();
        tracer.Counters?.Complete(resultTask, activity);
    }
}
