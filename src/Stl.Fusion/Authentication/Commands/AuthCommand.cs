using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    // Regular commands
    public record SignOutCommand(bool Force, Session Session)
        : ISessionCommand<Unit> { }

    // Server-side only!
    public record SetupSessionCommand(string IPAddress, string UserAgent, Session Session)
        : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand(Session session) : this("", "", session) { }
    }
    public record SignInCommand(User User, Session Session)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit> { }
}
