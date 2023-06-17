using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SignOutCommand: ISessionCommand<Unit>
{
    [DataMember, MemoryPackOrder(0)]
    public Session Session { get; init; } = Session.Null;
    [DataMember, MemoryPackOrder(1)]
    public string? KickUserSessionHash { get; init; }
    [DataMember, MemoryPackOrder(2)]
    public bool KickAllUserSessions { get; init; }
    [DataMember, MemoryPackOrder(3)]
    public bool Force { get; init; }

    public SignOutCommand(Session session, bool force = false)
    {
        Session = session;
        Force = force;
    }

    public SignOutCommand(Session session, string kickUserSessionHash, bool force = false)
    {
        Session = session;
        KickUserSessionHash = kickUserSessionHash;
        Force = force;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public SignOutCommand(Session session, string? kickUserSessionHash, bool kickAllUserSessions, bool force)
    {
        Session = session;
        KickUserSessionHash = kickUserSessionHash;
        KickAllUserSessions = kickAllUserSessions;
        Force = force;
    }
}
