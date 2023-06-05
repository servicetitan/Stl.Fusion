namespace Stl.Rpc.Infrastructure;

[DataContract]
public record RpcMessage(
    [property: DataMember(Order = 0)] long CallId,
    [property: DataMember(Order = 1)] string Service,
    [property: DataMember(Order = 2)] string Method,
    [property: DataMember(Order = 3)] TextOrBytes ArgumentData,
    [property: DataMember(Order = 4)] List<RpcHeader> Headers
);
