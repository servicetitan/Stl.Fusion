using System.Reactive;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record IsolatedRemoveCommand(Session Session, string Key) : ISessionCommand<Unit>
    {
        public IsolatedRemoveCommand() : this(Session.Null, "") { }
    }
}
