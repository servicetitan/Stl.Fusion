namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record SignInCommand(
    [property: DataMember] Session Session,
    [property: DataMember] User User,
    [property: DataMember] UserIdentity AuthenticatedIdentity
    ) : ISessionCommand<Unit>, IBackendCommand
{
    public SignInCommand() : this(Session.Null, null!, null!) { }
    public SignInCommand(Session session, User user)
        : this(session, user, user.Identities.Single().Key) { }
}
