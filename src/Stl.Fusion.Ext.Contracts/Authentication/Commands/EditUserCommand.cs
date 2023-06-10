namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record EditUserCommand(
    [property: DataMember] Session Session
    ) : ISessionCommand<Unit>
{
    [DataMember] public string? Name { get; init; }

    public EditUserCommand() : this(Session.Null) { }
    public EditUserCommand(Session session, string? name = null) : this(session)
        => Name = name;
}
