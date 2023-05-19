namespace Stl.Rpc.Infrastructure;

[DataContract]
public sealed record RpcMessage(
    [property: DataMember(Order = 1)] string Service,
    [property: DataMember(Order = 2)] string Method,
    [property: DataMember(Order = 3)] object? Arguments,
    List<RpcHeader>? Headers = null,
    [property: DataMember(Order = 16)] long CallId = 0)

{
    [DataMember(Order = 8)]
    public List<RpcHeader> Headers { get; init; } = Headers ?? new List<RpcHeader>();
}
