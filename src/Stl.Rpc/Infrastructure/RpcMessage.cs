namespace Stl.Rpc.Infrastructure;

public sealed record RpcMessage(
    string Service,
    string Method,
    object? Arguments,
    List<RpcHeader>? Headers = null)
{
    public List<RpcHeader> Headers { get; init; } = Headers ?? new List<RpcHeader>();
}
