namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record SetManyCommand(
    [property: DataMember] (string Key, string Value, Moment? ExpiresAt)[] Items
    ) : ICommand<Unit>, IBackendCommand
{
    public SetManyCommand() : this(Array.Empty<(string, string, Moment?)>()) { }
}
