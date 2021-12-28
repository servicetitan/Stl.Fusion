namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record SetSessionOptionsCommand(
    [property: DataMember] Session Session,
    [property: DataMember] ImmutableOptionSet Options,
    [property: DataMember] long? BaseVersion = null
    ) : ISessionCommand<Unit>
{
    public SetSessionOptionsCommand() : this(Session.Null, ImmutableOptionSet.Empty) { }
}
