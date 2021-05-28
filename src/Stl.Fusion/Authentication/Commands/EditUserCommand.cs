using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record EditUserCommand(Session Session, string? Name = null)
        : SessionCommandBase<Unit>(Session)
    {
        public EditUserCommand() : this(Session.Null) { }
    }
}
