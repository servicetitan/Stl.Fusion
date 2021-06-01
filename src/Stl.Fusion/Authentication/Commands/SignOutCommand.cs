using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record SignOutCommand(Session Session, bool Force = false)
        : SessionCommandBase<Unit>(Session)
    {
        public SignOutCommand() : this(Session.Null) { }
    }
}
