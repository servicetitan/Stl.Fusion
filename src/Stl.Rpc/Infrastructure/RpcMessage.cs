namespace Stl.Rpc.Infrastructure;

[DataContract]
public abstract record RpcMessage(
    [property: DataMember(Order = 0)] long CallId,
    [property: DataMember(Order = 1)] string Service,
    [property: DataMember(Order = 2)] string Method,
    [property: DataMember(Order = 7)] List<RpcHeader> Headers
);

[DataContract]
public sealed record RpcMessage<TArgumentData>(
    long CallId,
    string Service,
    string Method,
    [property: DataMember(Order = 3)] TArgumentData ArgumentData,
    List<RpcHeader> Headers
) : RpcMessage(CallId, Service, Method, Headers);
