using System.Diagnostics;
using System.Diagnostics.Metrics;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Diagnostics;

public class RpcMethodActivityTracer : RpcMethodTracer
{
    protected readonly object Lock = new();

    public string OperationName { get; init; }
    public ActivitySource ActivitySource { get; init; }
    public bool UseMeter { get; init; } = false;
    public Meter? Meter { get; protected set; }
    public Counter<long>? CallCount { get; protected set; }
    public Counter<long>? ErrorCount { get; protected set; }
    public Histogram<double>? CallDuration { get; protected set; }

    public RpcMethodActivityTracer(RpcMethodDef method) : base(method)
    {
        OperationName = $"rpc:{method.Name.Value}@{method.Service.Name.Value}";
        ActivitySource = GetType().GetActivitySource();
    }

    public override RpcMethodTrace? TryStartTrace(RpcInboundCall call)
    {
        if (UseMeter && Meter == null)
            CreateMeter();
        var activity = ActivitySource.StartActivity(OperationName);
        return activity == null ? null : new RpcMethodActivityTrace(this, activity);
    }

    protected void CreateMeter()
    {
        if (Meter != null) return;
        lock (Lock) {
            if (Meter != null) return;

            Meter = GetType().GetMeter();
            CallCount = Meter.CreateCounter<long>("Call count: " + OperationName);
            ErrorCount = Meter.CreateCounter<long>("Error count: " + OperationName);
            CallDuration = Meter.CreateHistogram<double>("Call duration: " + OperationName, "ms");
        }
    }
}
