using System.Linq;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    // Regular commands
    public record SignOutCommand(bool Force, Session Session)
        : ISessionCommand<Unit>
    {
        public SignOutCommand() : this(false, Session.Null) { }
    }

    public record EditUserCommand(string? Name, Session Session)
        : ISessionCommand<Unit>
    {
        public EditUserCommand() : this(null, Session.Null) { }
    }

    // Server-side only!
    public record SetupSessionCommand(string IPAddress, string UserAgent, Session Session)
        : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand() : this(Session.Null) { }
        public SetupSessionCommand(Session session) : this("", "", session) { }
    }

    public record SignInCommand(User User, UserIdentity AuthenticatedIdentity, Session Session)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit>
    {
        public SignInCommand() : this(null!, null!, Session.Null) { }
        public SignInCommand(User user, Session session)
            : this(user, user.Identities.Single().Key, session) { }
    }
}
