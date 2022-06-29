using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record SandboxedSetCommand(
    [property: DataMember] Session Session,
    [property: DataMember] (string Key, string Value, Moment? ExpiresAt)[] Items
    ) : ISessionCommand<Unit>
{
    public SandboxedSetCommand() : this(Session.Null, Array.Empty<(string, string, Moment?)>()) { }
}
