namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record RemoveManyCommand(
    [property: DataMember] params string[] Keys
    ) : ServerSideCommandBase<Unit>
{
    public RemoveManyCommand() : this(Array.Empty<string>()) { }
}
