using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SignInCommand : ISessionCommand<Unit>, IBackendCommand
{
    [DataMember, MemoryPackOrder(0)]
    public Session Session { get; init; }
    [DataMember, MemoryPackOrder(1)]
    public User User { get; init; }
    [DataMember, MemoryPackOrder(2)]
    public UserIdentity AuthenticatedIdentity { get; init; }

    public SignInCommand() : this(Session.Null, null!, null!) { }
    public SignInCommand(Session session, User user)
        : this(session, user, user.Identities.Single().Key) { }

    [MemoryPackConstructor]
    public SignInCommand(Session Session, User User, UserIdentity AuthenticatedIdentity)
    {
        this.Session = Session;
        this.User = User;
        this.AuthenticatedIdentity = AuthenticatedIdentity;
    }
}
