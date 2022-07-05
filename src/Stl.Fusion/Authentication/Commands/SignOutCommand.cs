namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record SignOutCommand: ISessionCommand<Unit>
{
    [DataMember]
    public Session Session { get; init; } = Session.Null;
    [DataMember]
    public string? KickedSessionHash { get; init; }
    [DataMember]
    public bool KickEverySession { get; init; }
    [DataMember]
    public bool Force { get; init; }

    public SignOutCommand() { }
    public SignOutCommand(Session session, bool force = false)
    {
        Session = session;
        Force = force;
    }
    public SignOutCommand(Session session, string kickedSessionHash, bool force = false)
    {
        Session = session;
        KickedSessionHash = kickedSessionHash;
        Force = force;
    }
}
