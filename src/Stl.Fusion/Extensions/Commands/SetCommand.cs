namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record SetCommand(
    [property: DataMember] string Key,
    [property: DataMember] string Value,
    [property: DataMember] Moment? ExpiresAt = null
    ) : BackendCommand<Unit>
{
    public SetCommand() : this("", "") { }
}
