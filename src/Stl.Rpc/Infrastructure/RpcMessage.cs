namespace Stl.Rpc.Infrastructure;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial class RpcMessage
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public byte CallTypeId;
    [DataMember(Order = 1), MemoryPackOrder(1)] public long CallId;
    [DataMember(Order = 2), MemoryPackOrder(2)] public string Service;
    [DataMember(Order = 3), MemoryPackOrder(3)] public string Method;
    [DataMember(Order = 4), MemoryPackOrder(4)] public TextOrBytes ArgumentData;
    [DataMember(Order = 5), MemoryPackOrder(5)] public List<RpcHeader>? Headers;

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
        return $"{nameof(RpcMessage)}({CallTypeId}:{CallId} -> '{Service}.{Method}', " +
            $"ArgumentData: {ArgumentData.ToString(16)}, " +
            $"Headers({headers.Count}): {headers.ToDelimitedString()})";
    }
}
