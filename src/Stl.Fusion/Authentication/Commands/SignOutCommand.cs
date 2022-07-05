namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record SignOutCommand: ISessionCommand<Unit>
{
    [DataMember]
    public Session Session { get; init; } = Session.Null;
    [DataMember]
    public string? KickUserSessionHash { get; init; }
    [DataMember]
    public bool KickAllUserSessions { get; init; }
    [DataMember]
    public bool Force { get; init; }

    public SignOutCommand() { }
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
}
