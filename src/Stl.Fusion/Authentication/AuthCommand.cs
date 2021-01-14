using System.Reactive;
using Stl.CommandR;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication
{
    public static class AuthCommand
    {
        // Regular commands
        public record SignOut(bool Force, Session Session)
            : ISessionCommand<Unit> { }

        // Server-side only!
        public record SaveSessionInfo(SessionInfo SessionInfo, Session Session)
            : ServerSideCommandBase<Unit>, ISessionCommand<Unit> { }
        public record SignIn(User User, Session Session)
            : ServerSideCommandBase<Unit>, ISessionCommand<Unit> { }
    }
}
