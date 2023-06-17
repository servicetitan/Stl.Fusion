using MemoryPack;

namespace Stl.Fusion.Extensions.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SandboxedSetCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] (string Key, string Value, Moment? ExpiresAt)[] Items
) : ISessionCommand<Unit>;
