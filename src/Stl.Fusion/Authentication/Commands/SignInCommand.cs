using System.Linq;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    public record SignInCommand(Session Session, User User, UserIdentity AuthenticatedIdentity)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit>
    {
        public SignInCommand() : this(Session.Null, null!, null!) { }
        public SignInCommand(Session session, User user)
            : this(session, user, user.Identities.Single().Key) { }
    }
}
