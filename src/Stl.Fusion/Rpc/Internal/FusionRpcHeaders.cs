using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public static class FusionRpcHeaders
{
    public static readonly RpcHeader ComputeMethod = new("->r->i");
    public static readonly RpcHeader Version = new("v");
}
