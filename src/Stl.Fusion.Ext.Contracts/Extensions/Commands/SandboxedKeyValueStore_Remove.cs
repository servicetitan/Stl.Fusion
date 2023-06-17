namespace Stl.Fusion.Extensions;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record SandboxedKeyValueStore_Remove(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] string[] Keys
) : ISessionCommand<Unit>;
