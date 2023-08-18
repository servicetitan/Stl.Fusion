namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record Auth_EditUser : ISessionCommand<Unit>
{
    [DataMember, MemoryPackOrder(0)]
    public Session Session { get; init; } = null!;
    [DataMember, MemoryPackOrder(1)]
    public string? Name { get; init; }

    public Auth_EditUser() { }

    [MemoryPackConstructor]
    public Auth_EditUser(Session session, string? name = null)
    {
        Session = session;
        Name = name;
    }
}
