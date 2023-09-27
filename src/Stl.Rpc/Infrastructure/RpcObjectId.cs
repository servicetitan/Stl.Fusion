using System.Globalization;

namespace Stl.Rpc.Infrastructure;

[StructLayout(LayoutKind.Sequential)] // Important!
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct RpcObjectId(
    [property: DataMember(Order = 0), MemoryPackOrder(0)] Guid HostId,
    [property: DataMember(Order = 1), MemoryPackOrder(1)] long LocalId)
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public bool IsNone => LocalId == 0 && HostId == default;

    public override string ToString()
        => $"{HostId:D}/{LocalId.ToString(CultureInfo.InvariantCulture)}";
}
