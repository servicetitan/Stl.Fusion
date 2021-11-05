namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record RemoveManyCommand(
    [property: DataMember] params string[] Keys
    ) : ICommand<Unit>, IBackendCommand
{
    public RemoveManyCommand() : this(Array.Empty<string>()) { }
}
