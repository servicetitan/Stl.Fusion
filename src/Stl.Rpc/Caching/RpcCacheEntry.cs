namespace Stl.Rpc.Caching;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial class RpcCacheEntry
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public RpcCacheKey Key { get; init; }
    [DataMember(Order = 1), MemoryPackOrder(1)] public TextOrBytes Result { get; init; }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RpcCacheEntry(RpcCacheKey key, TextOrBytes result)
    {
        Key = key;
        Result = result;
    }

    public override string ToString()
        => $"{nameof(RpcCacheEntry)}({Key} -> {Result.ToString(16)})";
}
