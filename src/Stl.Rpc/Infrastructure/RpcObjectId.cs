using System.Globalization;

namespace Stl.Rpc.Infrastructure;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct RpcObjectId(
    [property: DataMember(Order = 0), MemoryPackOrder(0)] Guid HostId,
    [property: DataMember(Order = 1), MemoryPackOrder(1)] long LocalId)
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public bool IsNone => LocalId == 0 && HostId == default;

    public override string ToString()
        => $"{HostId:D}/{LocalId.ToString(CultureInfo.InvariantCulture)}";
}
