namespace Stl.Fusion.Extensions;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record KeyValueStore_Remove(
    [property: DataMember, MemoryPackOrder(0)] Symbol TenantId,
    [property: DataMember, MemoryPackOrder(1)] string[] Keys
) : ICommand<Unit>, IBackendCommand;
