using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stl.Rpc.Diagnostics;

public class RpcMethodActivityCounters
{
    public readonly Meter Meter;
    public readonly Counter<long> CallCount;
    public readonly Counter<long> ErrorCount;
    public readonly Histogram<double> CallDuration;

    public RpcMethodActivityCounters(RpcMethodActivityTracer tracer)
    {
        var operationName = tracer.OperationName;
        Meter = tracer.GetType().GetMeter();
        CallCount = Meter.CreateCounter<long>("Call count: " + operationName);
        ErrorCount = Meter.CreateCounter<long>("Error count: " + operationName);
        CallDuration = Meter.CreateHistogram<double>("Call duration: " + operationName, "ms");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Complete(Task resultTask, Activity activity)
    {
        CallCount.Add(1);
        if (!resultTask.IsCompletedSuccessfully())
            ErrorCount.Add(1);
        CallDuration.Record(activity.Duration.TotalMilliseconds);
    }
}
