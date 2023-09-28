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
        tracer.CallCount?.Add(1);
        if (!resultTask.IsCompletedSuccessfully())
            tracer.ErrorCount?.Add(1);
        tracer.CallDuration?.Record(activity.Duration.TotalMilliseconds);
    }
}
