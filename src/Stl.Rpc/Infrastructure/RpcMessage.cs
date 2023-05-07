namespace Stl.Rpc.Infrastructure;

public sealed record RpcMessage(
    string Service,
    string Method,
    object? Arguments,
    List<RpcHeader>? Headers)
{
    public List<RpcHeader>? Headers { get; init; } = Headers?.Count > 0 ? Headers : null;
}
