namespace Stl.Rpc.Infrastructure;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
public partial class RpcMessage(
    byte callTypeId,
    long relatedId,
    string service,
    string method,
    TextOrBytes argumentData,
    List<RpcHeader>? headers)
{
    [JsonInclude, DataMember(Order = 0), MemoryPackOrder(0)] public byte CallTypeId = callTypeId;
    [JsonInclude, DataMember(Order = 1), MemoryPackOrder(1)] public long RelatedId = relatedId;
    [JsonInclude, DataMember(Order = 2), MemoryPackOrder(2)] public string Service = service;
    [JsonInclude, DataMember(Order = 3), MemoryPackOrder(3)] public string Method = method;
    [JsonInclude, DataMember(Order = 4), MemoryPackOrder(4)] public TextOrBytes ArgumentData = argumentData;
    [JsonInclude, DataMember(Order = 5), MemoryPackOrder(5)] public List<RpcHeader>? Headers = headers;

    public override string ToString()
    {
        var headers = Headers.OrEmpty();
        return $"{nameof(RpcMessage)} #{RelatedId}/{CallTypeId}: {Service}.{Method}, "
            + $"ArgumentData: {ArgumentData.ToString(16)}"
            + (headers.Count > 0 ? $", Headers: {headers.ToDelimitedString()}" : "");
    }
}
