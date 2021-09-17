using System.Runtime.Serialization;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    [DataContract]
    public record SetupSessionCommand(
        [property: DataMember] Session Session,
        [property: DataMember] string IPAddress = "",
        [property: DataMember] string UserAgent = ""
        ) : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand() : this(Session.Null) { }
    }
}
