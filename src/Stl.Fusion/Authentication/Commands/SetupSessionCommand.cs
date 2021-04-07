using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    public record SetupSessionCommand(Session Session, string IPAddress = "", string UserAgent = "")
        : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand() : this(Session.Null) { }
    }
}
