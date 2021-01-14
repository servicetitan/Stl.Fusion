using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Authentication.Commands
{
    // Regular commands
    public record SignOutCommand(bool Force, Session Session)
        : ISessionCommand<Unit> { }

    // Server-side only!
    public record SaveSessionInfoCommand(SessionInfo SessionInfo, Session Session)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit> { }
    public record SignInCommand(User User, Session Session)
        : ServerSideCommandBase<Unit>, ISessionCommand<Unit> { }
}
