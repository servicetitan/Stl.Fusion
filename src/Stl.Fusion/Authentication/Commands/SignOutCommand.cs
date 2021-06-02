using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record SignOutCommand(Session Session, bool Force = false)
        : ISessionCommand<Unit>
    {
        public SignOutCommand() : this(Session.Null) { }
    }
}
