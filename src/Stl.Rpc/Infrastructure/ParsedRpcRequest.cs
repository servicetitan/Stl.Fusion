using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public sealed record ParsedRpcRequest(
    Type Service,
    MethodInfo Method,
    ArgumentList Arguments,
    RpcHeader[] Headers);
