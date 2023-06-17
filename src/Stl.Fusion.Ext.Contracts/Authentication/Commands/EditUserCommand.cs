using MemoryPack;

namespace Stl.Fusion.Authentication.Commands;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record EditUserCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session
    ) : ISessionCommand<Unit>
{
    [DataMember, MemoryPackOrder(1)]
    public string? Name { get; init; }

    [MemoryPackConstructor]
    public EditUserCommand(Session session, string? name = null) : this(session)
        => Name = name;
}
