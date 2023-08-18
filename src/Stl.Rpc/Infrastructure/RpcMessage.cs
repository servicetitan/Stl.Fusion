namespace Stl.Rpc.Infrastructure;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial class RpcMessage
{
    [JsonInclude, DataMember(Order = 0), MemoryPackOrder(0)] public byte CallTypeId;
    [JsonInclude, DataMember(Order = 1), MemoryPackOrder(1)] public long CallId;
    [JsonInclude, DataMember(Order = 2), MemoryPackOrder(2)] public string Service;
    [JsonInclude, DataMember(Order = 3), MemoryPackOrder(3)] public string Method;
    [JsonInclude, DataMember(Order = 4), MemoryPackOrder(4)] public TextOrBytes ArgumentData;
    [JsonInclude, DataMember(Order = 5), MemoryPackOrder(5)] public List<RpcHeader>? Headers;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RpcMessage(
        byte callTypeId, long callId,
        string service, string method,
        TextOrBytes argumentData,
        List<RpcHeader>? headers)
    {
        CallTypeId = callTypeId;
        CallId = callId;
        Service = service;
        Method = method;
        ArgumentData = argumentData;
        Headers = headers;
    }

    public override string ToString()
    {
        var headers = Headers.OrEmpty();
        return $"{nameof(RpcMessage)} #{CallId}/{CallTypeId}: {Service}.{Method}, "
            + $"ArgumentData: {ArgumentData.ToString(16)}"
            + (headers.Count > 0 ? $", Headers: {headers.ToDelimitedString()}" : "");
    }
}
