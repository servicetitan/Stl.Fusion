using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public sealed record RpcBoundRequest(
    RpcMethodDef MethodDef,
    ArgumentList Arguments,
    Task? ResultTask = null)
{
    public List<RpcHeader> Headers { get; init; } = new();
}
