namespace Stl.Fusion.Extensions;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record KeyValueStore_Set(
    [property: DataMember, MemoryPackOrder(0)] Symbol TenantId,
    [property: DataMember, MemoryPackOrder(1)] (string Key, string Value, Moment? ExpiresAt)[] Items
) : ICommand<Unit>, IBackendCommand;
