namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record SetCommand(
    [property: DataMember] Symbol TenantId,
    [property: DataMember] (string Key, string Value, Moment? ExpiresAt)[] Items
    ) : ICommand<Unit>, IBackendCommand
{
    public SetCommand() : this(default, Array.Empty<(string, string, Moment?)>()) { }
}
