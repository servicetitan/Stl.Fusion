using MemoryPack;

namespace Stl.Fusion.Extensions.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record RemoveCommand(
    [property: DataMember, MemoryPackOrder(0)] Symbol TenantId,
    [property: DataMember, MemoryPackOrder(1)] string[] Keys
) : ICommand<Unit>, IBackendCommand;
