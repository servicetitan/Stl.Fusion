using MemoryPack;

namespace Stl.Fusion.Extensions.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SetCommand(
    [property: DataMember, MemoryPackOrder(0)] Symbol TenantId,
    [property: DataMember, MemoryPackOrder(1)] (string Key, string Value, Moment? ExpiresAt)[] Items
) : ICommand<Unit>, IBackendCommand;
