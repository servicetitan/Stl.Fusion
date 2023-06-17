using MemoryPack;

namespace Stl.Fusion.Extensions.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SandboxedRemoveCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] string[] Keys
) : ISessionCommand<Unit>;
