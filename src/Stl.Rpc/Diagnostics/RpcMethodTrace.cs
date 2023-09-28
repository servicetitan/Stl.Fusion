using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Diagnostics;

public abstract class RpcMethodTrace
{
    public abstract void OnResultTaskReady(RpcInboundCall call);
}
