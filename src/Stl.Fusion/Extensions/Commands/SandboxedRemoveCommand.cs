using System.Reactive;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedRemoveCommand(Session Session, string Key) : ISessionCommand<Unit>
    {
        public SandboxedRemoveCommand() : this(Session.Null, "") { }
    }
}
