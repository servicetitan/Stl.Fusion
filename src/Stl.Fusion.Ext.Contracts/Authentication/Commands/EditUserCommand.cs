using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record EditUserCommand : ISessionCommand<Unit>
{
    [DataMember, MemoryPackOrder(0)]
    public Session Session { get; init; } = Session.Null;

    [DataMember, MemoryPackOrder(1)]
    public string? Name { get; init; }

    public EditUserCommand() { }

    [MemoryPackConstructor]
    public EditUserCommand(Session session, string? name = null) : this(session)
        => Name = name;
}
