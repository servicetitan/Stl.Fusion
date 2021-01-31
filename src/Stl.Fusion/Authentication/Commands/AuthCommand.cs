using System.Linq;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    // Regular commands
    public record SignOutCommand(bool Force, Session Session)
        : ISessionCommand<Unit> { }
    public record EditUserCommand(string? Name, Session Session)
        : ISessionCommand<Unit> { }

    // Server-side only!
    public record SetupSessionCommand(string IPAddress, string UserAgent, Session Session)
        : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand(Session session) : this("", "", session) { }
    }
    public record SignInCommand(User User, UserIdentity AuthenticatedIdentity, Session Session)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit>
    {
        public SignInCommand(User user, Session session) : this(user, user.Identities.Single().Key, session) { }
    }
}
