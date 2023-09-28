using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Diagnostics;

public abstract class RpcMethodTracer(RpcMethodDef method)
{
    public RpcMethodDef Method { get; init; } = method;
    public Sampler Sampler { get; init; } = Sampler.Always;

    public abstract RpcMethodTrace? TryStartTrace(RpcInboundCall call);
}
