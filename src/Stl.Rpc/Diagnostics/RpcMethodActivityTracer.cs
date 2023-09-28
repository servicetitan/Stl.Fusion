using System.Diagnostics;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Diagnostics;

public class RpcMethodActivityTracer : RpcMethodTracer
{
    protected readonly object Lock = new();

    public string OperationName { get; init; }
    public ActivitySource ActivitySource { get; init; }
    public bool UseCounters { get; init; } = false;
    public RpcMethodActivityCounters? Counters { get; protected set; }

    public RpcMethodActivityTracer(RpcMethodDef method) : base(method)
    {
        OperationName = $"rpc:{method.Name.Value}@{method.Service.Name.Value}";
        ActivitySource = GetType().GetActivitySource();
    }

    public override RpcMethodTrace? TryStartTrace(RpcInboundCall call)
    {
        if (UseCounters && Counters == null)
            lock (Lock)
                Counters ??= new RpcMethodActivityCounters(this);
        var activity = ActivitySource.StartActivity(OperationName);
        return activity == null ? null : new RpcMethodActivityTrace(this, activity);
    }
}
