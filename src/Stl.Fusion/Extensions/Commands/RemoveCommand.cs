namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record RemoveCommand(
    [property: DataMember] string Key
    ) : BackendCommand<Unit>
{
    public RemoveCommand() : this("") { }
}
