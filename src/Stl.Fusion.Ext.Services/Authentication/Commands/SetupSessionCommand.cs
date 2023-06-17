using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SetupSessionCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] string IPAddress = "",
    [property: DataMember, MemoryPackOrder(2)] string UserAgent = "",
    [property: DataMember, MemoryPackOrder(3)] ImmutableOptionSet Options = default
) : ISessionCommand<SessionInfo>, IBackendCommand, INotLogged;
