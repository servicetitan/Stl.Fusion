namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record RemoveCommand(
    [property: DataMember] string Key
    ) : ICommand<Unit>, IBackendCommand
{
    public RemoveCommand() : this("") { }
}
