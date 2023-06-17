using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SetSessionOptionsCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] ImmutableOptionSet Options,
    [property: DataMember, MemoryPackOrder(2)] long? ExpectedVersion = null
) : ISessionCommand<Unit>;
