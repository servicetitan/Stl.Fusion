using System.Linq;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    // Regular commands
    public record SignOutCommand(Session Session, bool Force = false)
        : ISessionCommand<Unit>
    {
        public SignOutCommand() : this(Session.Null) { }
    }

    public record EditUserCommand(Session Session, string? Name = null)
        : ISessionCommand<Unit>
    {
        public EditUserCommand() : this(Session.Null) { }
    }

    // Server-side only!
    public record SetupSessionCommand(Session Session, string IPAddress = "", string UserAgent = "")
        : ServerSideCommandBase<SessionInfo>, ISessionCommand<SessionInfo>
    {
        public SetupSessionCommand() : this(Session.Null) { }
    }

    public record SignInCommand(Session Session, User User, UserIdentity AuthenticatedIdentity)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit>
    {
        public SignInCommand() : this(Session.Null, null!, null!) { }
        public SignInCommand(Session session, User user)
            : this(session, user, user.Identities.Single().Key) { }
    }
}
