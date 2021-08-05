using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record EditUserCommand(Session Session)
        : ISessionCommand<Unit>
    {
        public string? Name { get; init; }

        public EditUserCommand() : this(Session.Null) { }
        public EditUserCommand(Session session, string? name = null) : this(session)
            => Name = name;
    }
}
