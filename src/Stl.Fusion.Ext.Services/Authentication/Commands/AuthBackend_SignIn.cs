namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: MemoryPackConstructor]
// ReSharper disable once InconsistentNaming
public partial record AuthBackend_SignIn(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] User User,
    [property: DataMember, MemoryPackOrder(2)] UserIdentity AuthenticatedIdentity
    ) : ISessionCommand<Unit>, IBackendCommand
{
    public AuthBackend_SignIn() : this(null!, null!, null!) { }
    public AuthBackend_SignIn(Session session, User user)
        : this(session, user, user.Identities.Single().Key) { }
}
