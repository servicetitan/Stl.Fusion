namespace Stl.Rpc.Caching;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
public partial class RpcCacheEntry(RpcCacheKey key, TextOrBytes result)
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public RpcCacheKey Key { get; init; } = key;
    [DataMember(Order = 1), MemoryPackOrder(1)] public TextOrBytes Result { get; init; } = result;

    public override string ToString()
        => $"{nameof(RpcCacheEntry)}({Key} -> {Result.ToString(16)})";
}
