namespace Stl.Rpc.Caching;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
public sealed partial class RpcCacheEntry(RpcCacheKey key, TextOrBytes data)
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public RpcCacheKey Key { get; init; } = key;
    [DataMember(Order = 1), MemoryPackOrder(1)] public TextOrBytes Data { get; init; } = data;

    public void Deconstruct(out RpcCacheKey key, out TextOrBytes data)
    {
        key = Key;
        data = Data;
    }

    public override string ToString()
        => $"{nameof(RpcCacheEntry)}({Key} -> {Data.ToString(16)})";
}
