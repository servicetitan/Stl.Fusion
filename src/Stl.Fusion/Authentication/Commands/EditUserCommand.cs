using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record EditUserCommand(Session Session, string? Name = null)
        : ISessionCommand<Unit>
    {
        public EditUserCommand() : this(Session.Null) { }
    }
}
