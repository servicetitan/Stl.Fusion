namespace Stl.Fusion.Authentication.Commands;

[DataContract]
public record SetupSessionCommand(
    [property: DataMember] Session Session,
    [property: DataMember] string IPAddress = "",
    [property: DataMember] string UserAgent = "",
    [property: DataMember] ImmutableOptionSet Options = default
    ) : ISessionCommand<SessionInfo>, IBackendCommand, INotLogged
{
    public SetupSessionCommand() : this(Session.Null) { }
}
