namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record AuthBackend_SetupSession(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] string IPAddress = "",
    [property: DataMember, MemoryPackOrder(2)] string UserAgent = "",
    [property: DataMember, MemoryPackOrder(3)] ImmutableOptionSet Options = default
) : ISessionCommand<SessionInfo>, IBackendCommand, INotLogged;
