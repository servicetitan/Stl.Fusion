using MemoryPack;

namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record AuthBackend_SignIn : ISessionCommand<Unit>, IBackendCommand
{
    [DataMember, MemoryPackOrder(0)]
    public Session Session { get; init; }
    [DataMember, MemoryPackOrder(1)]
    public User User { get; init; }
    [DataMember, MemoryPackOrder(2)]
    public UserIdentity AuthenticatedIdentity { get; init; }

    public AuthBackend_SignIn() : this(Session.Null, null!, null!) { }
    public AuthBackend_SignIn(Session session, User user)
        : this(session, user, user.Identities.Single().Key) { }

    [MemoryPackConstructor]
    public AuthBackend_SignIn(Session Session, User User, UserIdentity AuthenticatedIdentity)
    {
        this.Session = Session;
        this.User = User;
        this.AuthenticatedIdentity = AuthenticatedIdentity;
    }
}
