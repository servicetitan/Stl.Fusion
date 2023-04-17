namespace Stl.Rpc.Infrastructure;

public sealed record RpcRequest(
    string Service,
    string Method,
    object? Arguments,
    RpcHeader[]? Headers = null);
