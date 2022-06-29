using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record SandboxedRemoveCommand(
    [property: DataMember] Session Session,
    [property: DataMember] params string[] Keys
    ) : ISessionCommand<Unit>
{
    public SandboxedRemoveCommand() : this(Session.Null, Array.Empty<string>()) { }
}
